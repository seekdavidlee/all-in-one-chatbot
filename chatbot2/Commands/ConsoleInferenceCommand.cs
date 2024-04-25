using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace chatbot2.Commands;

public class ConsoleInferenceCommand : ICommandAction
{
    private readonly ILanguageModel languageModel;
    private readonly IVectorDb vectorDb;
    public ConsoleInferenceCommand(IEnumerable<ILanguageModel> languageModels, IEnumerable<IVectorDb> vectorDbs)
    {
        languageModel = languageModels.GetSelectedLanguageModel();
        vectorDb = vectorDbs.GetSelectedVectorDb();
    }

    public string Name => "chatbot";

    public async Task ExecuteAsync(IConfiguration argsConfiguration)
    {
        await vectorDb.InitAsync();

        while (true)
        {
            Console.Write("Ask a question.\nUser: ");
            string userInput = Console.ReadLine() ?? "";

            if (userInput == "exit")
            {
                break;
            }

            var intentPrompt = await Util.GetResourceAsync("DetermineIntent.txt");
            intentPrompt = intentPrompt.Replace("{{$previous_intent}}", "");
            intentPrompt = intentPrompt.Replace("{{$query}}", userInput);

            var intentResponse = await languageModel.GetChatCompletionsAsync(intentPrompt);

            if (intentResponse is null)
            {
                throw new Exception("did not get response from llm");
            }

            const string keywordMarker = "Single intents:";
            var findIndex = intentResponse.IndexOf(keywordMarker, StringComparison.OrdinalIgnoreCase);
            if (findIndex < 0)
            {
                throw new Exception("did not find single intent in response");
            }
            intentResponse = intentResponse.Substring(findIndex + keywordMarker.Length);
            var lastIndex = intentResponse.IndexOf("]", 0, StringComparison.OrdinalIgnoreCase);
            intentResponse = intentResponse.Substring(0, lastIndex + 1);
            var parsedIntents = JsonSerializer.Deserialize<string[]>(intentResponse);
            if (parsedIntents is null)
            {
                throw new Exception("response did not deserialize properly");
            }

            var intent = parsedIntents.Length > 0 ? parsedIntents.Single() : userInput;

            var results = (await vectorDb.SearchAsync(intent)).ToArray();
            var replyPrompt = await Util.GetResourceAsync("DetermineReply.txt");
            replyPrompt = replyPrompt.Replace("{{$conversation}}", "");
            StringBuilder sb = new();
            for (var i = 0; i < results.Length; i++)
            {
                var result = results[i];
                sb.AppendLine($"doc[{i}]\n{result.Text}\n");
            }
            replyPrompt = replyPrompt.Replace("{{$documentation}}", sb.ToString());
            replyPrompt = replyPrompt.Replace("{{$user_query}}", intent);

            var replyResponse = await languageModel.GetChatCompletionsAsync(replyPrompt);

            if (replyResponse is null)
            {
                throw new Exception("did not get response from llm");
            }

            Console.WriteLine(replyResponse);
        }
    }
}
