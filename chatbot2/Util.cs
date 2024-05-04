using chatbot2.Configuration;
using chatbot2.Inferences;
using chatbot2.Ingestions;
using chatbot2.VectorDbs;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Threading.Tasks.Dataflow;

namespace chatbot2;

public static class Util
{
    public const string BlobPrefix = "blob:";

    public static Task<string> GetResourceAsync(string resourceName)
    {
        using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"chatbot2.Prompts.{resourceName}");
        using var streamReader = new StreamReader(resourceStream ?? throw new Exception($"Invalid {resourceName} stream"));
        return streamReader.ReadToEndAsync();
    }

    public static IEmbedding GetSelectedEmbedding(this IEnumerable<IEmbedding> embeddings, IConfig config)
    {
        return embeddings.Single(x => x.GetType().Name == config.EmbeddingType);
    }

    public static IVectorDb GetSelectedVectorDb(this IEnumerable<IVectorDb> vectorDbs)
    {
        var vectorDbType = Environment.GetEnvironmentVariable("VectorDbType") ?? throw new Exception("Missing VectorDbType!");
        return vectorDbs.Single(x => x.GetType().Name == vectorDbType);
    }

    public static ILanguageModel GetSelectedLanguageModel(this IEnumerable<ILanguageModel> languageModels)
    {
        var languageModelType = Environment.GetEnvironmentVariable("LanguageModelType") ?? throw new Exception("Missing LanguageModelType!");
        return languageModels.Single(x => x.GetType().Name == languageModelType);
    }

    public static string FullBody(this IndexedDocument[] docs)
    {
        StringBuilder sb = new();
        for (var i = 0; i < docs.Length; i++)
        {
            var result = docs[i];
            sb.AppendLine($"doc[{i}]\n{result.Text}\n");
        }
        return sb.ToString();
    }
    public static ExecutionDataflowBlockOptions GetDataflowOptions(this IConfig config, CancellationToken cancellationToken, int? overrideConcurrency = null)
    {
        return new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = overrideConcurrency ?? config.Concurrency,
            TaskScheduler = TaskScheduler.Default,
            CancellationToken = cancellationToken
        };
    }

    public static List<SearchModelDto> GetSearchModels(this ConcurrentBag<SearchModelDto> bagSearchModels, int startIndex, int endIndex)
    {
        return bagSearchModels.Skip(startIndex).Take(endIndex - startIndex).ToList();
    }

    public static IIngestionProcessor GetIngestionProcessor(this IEnumerable<IIngestionProcessor> processors, IConfig config)
    {
        return processors.Single(x => x.GetType().Name == config.IngestionProcessorType);
    }

    public static IInferenceWorkflow GetInferenceWorkflow(this IEnumerable<IInferenceWorkflow> inferenceWorkflows, IConfig config)
    {
        return inferenceWorkflows.Single(x => x.GetType().Name == config.InferenceProcessorType);
    }
}