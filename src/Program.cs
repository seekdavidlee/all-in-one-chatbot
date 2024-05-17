using AIOChatbot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AIOChatbot.Embeddings;
using AIOChatbot.VectorDbs;
using AIOChatbot.Llms;
using AIOChatbot.Ingestions;
using AIOChatbot.Commands;
using AIOChatbot.Evals;
using AIOChatbot.Inferences;
using System.Diagnostics;
using AIOChatbot.Configurations;
using AIOChatbot.Logging;
using Microsoft.Extensions.Logging;
using AIOChatbot.Inferences.Steps;
using Azure.Identity;

// support the use of .env file
DotNetEnv.Env.Load();

IConfiguration argsConfig = new ConfigurationBuilder()
       .AddCommandLine(args)
       .AddEnvironmentVariables()
       .Build();

var cmdName = argsConfig["AIOCommand"];
if (cmdName is null)
{
    Console.WriteLine("AIOCommand is missing");
    return -1;
}

// add config
Config config;

try
{
    config = new Config(new NetBricks.Config());
}
catch (CredentialUnavailableException)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("provided credentials is not valid, ensure environment variable INCLUDE_CREDENTIAL_TYPES is set correctly");
    Console.ResetColor();
    return -4;
}

config.Validate(cmdName);

var services = new ServiceCollection();
services.AddSingleton<IConfig>(config);
services.AddHttpClient();
services.AddSingleton<FileCache>();


if (cmdName == ImportGroundTruthsCommand.Command)
{
    services.AddImportGroundTruthsCommand();
}
else
{
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
        services.AddSingleton(typeof(IIngestionDataSource), Type.GetType(ingestionType) ?? throw new Exception($"invalid IVectorDbIngestion type {ingestionType}"));
    }
    services.AddSingleton<ICommandAction, ConsoleChatbotCommand>();
    services.AddSingleton<ICommandAction, IngestCommand>();
    services.AddSingleton<ICommandAction, DeleteSearchCommand>();
    services.AddSingleton<ICommandAction, LocalEvaluationCommand>();
    services.AddSingleton<ICommandAction, EvaluationSummarizeCommand>();
    services.AddSingleton<ICommandAction, ShowEvaluationMetricResultCommand>();
    services.AddSingleton<ICommandAction, ProcessQueueIngestionCommand>();
    services.AddSingleton<ICommandAction, ImportMetricsCommand>();
    services.AddSingleton<ICommandAction, RemoteEvaluationCommand>();
    services.AddSingleton<ICommandAction, ProcessQueueEvaluationCommand>();
    services.AddSingleton<ICommandAction, ProcessQueueInferenceCommand>();
    services.AddSingleton<ICommandAction, HttpChatbotCommand>();
    services.AddSingleton<EvaluationRunner>();

    services.AddSingleton<IInferenceWorkflow, SKInferenceWorkflow>();
    services.AddSingleton<IInferenceWorkflow, InferenceWorkflowQueue>();
    services.AddSingleton<IInferenceWorkflowStep, DetermineIntentStep>();
    services.AddSingleton<IInferenceWorkflowStep, RetrievedDocumentsStep>();
    services.AddSingleton<IInferenceWorkflowStep, DetermineReplyStep>();
    services.AddSingleton<EvaluationMetricWorkflow>();

    services.AddSingleton<ReportRepository>();
    services.AddSingleton<EvaluationSummarizeWorkflow>();
    services.AddSingleton<IngestionReporter>();
    services.AddSingleton<IIngestionProcessor, IngestionQueueService>();
    services.AddSingleton<IIngestionProcessor, IngestionProcessor>();
    services.AddSK();
}

var (traceProvider, meterProvider) = services.AddDiagnosticsServices(config, DiagnosticServices.Source.Name);

var provider = services.BuildServiceProvider();
var command = provider.GetServices<ICommandAction>().SingleOrDefault(c => c.Name == cmdName);
if (command is not null)
{
    var logger = provider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Command started at: {commandStarted}", DateTime.UtcNow);
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

    Stopwatch? sw = null;
    if (!command.LongRunning)
    {
        sw = new Stopwatch();
        sw.Start();
    }

    int returnCode;
    try
    {
        await command.ExecuteAsync(argsConfig, cancellationToken: cts.Token);
        returnCode = 0;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error executing command '{commandName}'", command.Name);
        returnCode = -3;
    }
    finally
    {
        if (sw is not null)
        {
            sw.Stop();
            logger.LogInformation("Operation '{commandName}' completed in {commandElapsedMilliseconds}ms", command.Name, sw.ElapsedMilliseconds);
        }

    }
    meterProvider.Dispose();
    traceProvider.Dispose();
    return returnCode;
}
else
{
    Console.WriteLine($"Command '{cmdName}' not found");
    return -2;
}


