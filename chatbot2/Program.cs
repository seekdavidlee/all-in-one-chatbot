// See https://aka.ms/new-console-template for more information
// C:\Users\seekd\.cache\lm-studio\models\easynet\microsoft-phi-2-GGUF\phi-2.Q6_K.gguf
using LLama.Common;
using LLama;
using chatbot2;
using System.Diagnostics;

using var vectorDb = new VectorDbClient();
await vectorDb.InitAsync();

// await vectorDb.SearchAsync("what is a container?");

var dataSourcePath = Environment.GetEnvironmentVariable("DataSourcePath") ?? throw new Exception("Missing DataSourcePath!");
var htmlReader = new HtmlReader();

foreach (var page in await htmlReader.ReadFilesAsync(dataSourcePath))
{
    Console.WriteLine($"processing page: {page.Context.PagePath}...");
    foreach (var section in page.Sections)
    {
        Console.WriteLine($"processing section: {section.IdPrefix}...");
        Console.WriteLine(section);
        // await vectorDb.ProcessAsync(section);
    }
}

Console.WriteLine("done!");
return;
////var result = await Embedding.GetEmbeddingsAsync("Hello, world!");

//string modelPath = Environment.GetEnvironmentVariable("ModelFilePath") ?? throw new Exception("Missing ModelFilePath!"); // change it to your own model path.

//var parameters = new ModelParams(modelPath)
//{
//    ContextSize = 1024, // The longest length of chat as memory.
//    GpuLayerCount = 5 // How many layers to offload to GPU. Please adjust it according to your GPU memory.
//};
//using var model = LLamaWeights.LoadFromFile(parameters);
//using var context = model.CreateContext(parameters);
//var executor = new InteractiveExecutor(context);

//// Add chat histories as prompt to tell AI how to act.
//var chatHistory = new ChatHistory();
//chatHistory.AddMessage(AuthorRole.System, "Transcript of a dialog, where the User interacts with an Assistant named Bob. Bob is helpful, kind, honest, good at writing, and never fails to answer the User's requests immediately and with precision.");
//chatHistory.AddMessage(AuthorRole.User, "Hello, Bob.");
//chatHistory.AddMessage(AuthorRole.Assistant, "Hello. How may I help you today?");

//ChatSession session = new(executor, chatHistory);

//InferenceParams inferenceParams = new()
//{
//    MaxTokens = 256, // No more than 256 tokens should appear in answer. Remove it if antiprompt is enough for control.
//    AntiPrompts = new List<string> { "User:" }, // Stop generation once antiprompts appear.
//    Temperature = 0,
//    TopK = 10,
//    TopP = 0.1F
//};

//Console.ForegroundColor = ConsoleColor.Yellow;
//Console.Write("The chat session has started.\nUser: ");
//Console.ForegroundColor = ConsoleColor.Green;
//string userInput = Console.ReadLine() ?? "";

//while (userInput != "exit")
//{
//    await foreach ( // Generate the response streamingly.
//        var text
//        in session.ChatAsync(
//            new ChatHistory.Message(AuthorRole.User, userInput),
//            inferenceParams))
//    {
//        Console.ForegroundColor = ConsoleColor.White;
//        Console.Write(text);
//    }
//    Console.ForegroundColor = ConsoleColor.Green;
//    userInput = Console.ReadLine() ?? "";
//}
