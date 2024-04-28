using chatbot2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using chatbot2.Embeddings;
using chatbot2.VectorDbs;
using chatbot2.Llms;
using chatbot2.Ingestions;
using Microsoft.Extensions.Logging;
using chatbot2.Commands;
using chatbot2.Evals;
using chatbot2.Inferences;
using System.Diagnostics;

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

//services.AddSingleton<IEmbedding, LocalEmbedding>();
//services.AddSingleton<IVectorDb, ChromaDbClient>();
//services.AddSingleton<ILanguageModel, LocalLLM>();
services.AddSingleton<IEmbedding, AzureOpenAIEmbedding>();
services.AddSingleton<IVectorDb, AzureAISearch>();
services.AddSingleton<ILanguageModel, AzureOpenAIClient>();
services.AddSingleton<IRestClientAuthHeaderProvider, CustomAuthProvider>();
services.AddSingleton<IVectorDbIngestion, LocalDirectoryIngestion>();
services.AddSingleton<IVectorDbIngestion, RestApiIngestion>();
services.AddSingleton<ICommandAction, ConsoleChatbotCommand>();
services.AddSingleton<ICommandAction, IngestCommand>();
services.AddSingleton<ICommandAction, DeleteSearchCommand>();
services.AddSingleton<ICommandAction, EvaluationCommand>();
services.AddSingleton<ICommandAction, EvaluationSummarizeCommand>();
services.AddSingleton<ICommandAction, ShowEvaluationMetricResultCommand>();
services.AddSingleton<GroundTruthIngestion>();
services.AddSingleton<IGroundTruthReader, ExcelGrouthTruthReader>();
services.AddSingleton<InferenceWorkflow>();
services.AddSingleton<EvaluationMetricWorkflow>();
services.AddSingleton<FileCache>();
services.AddSingleton<ReportRepository>();
services.AddSingleton<EvaluationSummarizeWorkflow>();

var provider = services.BuildServiceProvider();
foreach (var command in provider.GetServices<ICommandAction>())
{
    if (command.Name == argsConfig["command"])
    {
        var sw = new Stopwatch();
        sw.Start();
        try
        {
            await command.ExecuteAsync(argsConfig);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
        }
        finally
        {
            sw.Stop();
        }

        Console.WriteLine($"Operation '{command.Name}' completed in {sw.ElapsedMilliseconds}ms");
        return;
    }
}


