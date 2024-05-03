using chatbot2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using chatbot2.Embeddings;
using chatbot2.VectorDbs;
using chatbot2.Llms;
using chatbot2.Ingestions;
using chatbot2.Commands;
using chatbot2.Evals;
using chatbot2.Inferences;
using System.Diagnostics;
using chatbot2.Configuration;
using chatbot2.Logging;
using Microsoft.Extensions.Logging;

// add config
var netConfig = new NetBricks.Config();
var config = new Config(netConfig);
config.Validate();

IConfiguration argsConfig = new ConfigurationBuilder()
       .AddCommandLine(args)
       .AddEnvironmentVariables()
       .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfig>(config);
services.AddHttpClient();

services.AddSingleton<IEmbedding, LocalEmbedding>();
services.AddSingleton<IVectorDb, ChromaDbClient>();
services.AddSingleton<ILanguageModel, LocalLLM>();
services.AddSingleton<IEmbedding, AzureOpenAIEmbedding>();
services.AddSingleton<IVectorDb, AzureAISearch>();
services.AddSingleton<ILanguageModel, AzureOpenAIClient>();
services.AddSingleton<IRestClientAuthHeaderProvider, CustomAuthProvider>();

foreach (var ingestionType in config.IngestionTypes)
{
    if (ingestionType is null)
    {
        continue;
    }
    services.AddSingleton(typeof(IVectorDbIngestion), Type.GetType(ingestionType) ?? throw new Exception($"invalid IVectorDbIngestion type {ingestionType}"));
}
services.AddSingleton<ICommandAction, ChatbotCommand>();
services.AddSingleton<ICommandAction, IngestCommand>();
services.AddSingleton<ICommandAction, DeleteSearchCommand>();
services.AddSingleton<ICommandAction, LocalEvaluationCommand>();
services.AddSingleton<ICommandAction, EvaluationSummarizeCommand>();
services.AddSingleton<ICommandAction, ShowEvaluationMetricResultCommand>();
services.AddSingleton<ICommandAction, ProcessQueueIngestionCommand>();
services.AddSingleton<ICommandAction, ImportGroundTruthsCommand>();
services.AddSingleton<ICommandAction, ImportMetricsCommand>();
services.AddSingleton<ICommandAction, RemoteEvaluationCommand>();
services.AddSingleton<ICommandAction, ProcessQueueEvaluationCommand>();
services.AddSingleton<EvaluationRunner>();
services.AddSingleton<GroundTruthIngestion>();
services.AddSingleton<IGroundTruthReader, ExcelGrouthTruthReader>();
services.AddSingleton<InferenceWorkflow>();
services.AddSingleton<EvaluationMetricWorkflow>();
services.AddSingleton<FileCache>();
services.AddSingleton<ReportRepository>();
services.AddSingleton<EvaluationSummarizeWorkflow>();
services.AddSingleton<IngestionReporter>();
services.AddSingleton<IIngestionProcessor, QueueService>();
services.AddSingleton<IIngestionProcessor, IngestionProcessor>();

var (traceProvider, meterProvider) = services.AddDiagnosticsServices(config, DiagnosticServices.Source.Name);

var cmdName = argsConfig["command"];
var provider = services.BuildServiceProvider();
var command = provider.GetServices<ICommandAction>().SingleOrDefault(c => c.Name == cmdName);
if (command is not null)
{
    var logger = provider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Command started at: {0}", DateTime.UtcNow);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("press Ctrl+C to stop...");
    Console.ResetColor();

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) =>
    {
        Console.WriteLine("\nuser cancelled");
        e.Cancel = true;
        cts.Cancel();
    };

    var sw = new Stopwatch();
    sw.Start();
    try
    {
        await command.ExecuteAsync(argsConfig, cancellationToken: cts.Token);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error executing command '{commandName}'", command.Name);
    }
    finally
    {
        sw.Stop();
    }

    logger.LogInformation("Operation '{commandName}' completed in {commandElapsedMilliseconds}ms", command.Name, sw.ElapsedMilliseconds);

    meterProvider.Dispose();
    traceProvider.Dispose();
}
else
{
    Console.WriteLine($"Command '{cmdName}' not found");
}


