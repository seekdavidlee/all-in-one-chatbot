using chatbot2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using chatbot2.Embeddings;
using chatbot2.VectorDbs;
using chatbot2.Llms;
using chatbot2.Ingestions;
using Microsoft.Extensions.Logging;
using chatbot2.Commands;

IConfiguration argsConfig = new ConfigurationBuilder()
       .AddCommandLine(args)
       .Build();

var services = new ServiceCollection();
services.AddHttpClient();
services.AddLogging(c =>
{
    var level = Environment.GetEnvironmentVariable("LogLevel") ?? "Information";
    c.SetMinimumLevel((LogLevel)Enum.Parse(typeof(LogLevel), level));
    c.AddConsole();
});
services.AddSingleton<IEmbedding, AzureOpenAIEmbedding>();
services.AddSingleton<IEmbedding, LocalEmbedding>();
services.AddSingleton<IVectorDb, AzureAISearch>();
services.AddSingleton<IVectorDb, ChromaDbClient>();
services.AddSingleton<ILanguageModel, LocalLLM>();
services.AddSingleton<ILanguageModel, AzureOpenAIClient>();
services.AddSingleton<ILanguageModel, LocalLLM>();
services.AddSingleton<IRestClientAuthHeaderProvider, CustomAuthProvider>();
//services.AddSingleton<IVectorDbIngestion, LocalDirectoryIngestion>();
services.AddSingleton<IVectorDbIngestion, RestApiIngestion>();
services.AddSingleton<ICommandAction, ConsoleInferenceCommand>();
services.AddSingleton<ICommandAction, IngestCommand>();
services.AddSingleton<ICommandAction, DeleteSearchCommand>();

var provider = services.BuildServiceProvider();
foreach (var command in provider.GetServices<ICommandAction>())
{
    if (command.Name == argsConfig["command"])
    {
        await command.ExecuteAsync(argsConfig);
        return;
    }
}


