using System.Reflection;

namespace chatbot2;

public static class Util
{
    public static Task<string> GetResourceAsync(string resourceName)
    {
        using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"chatbot2.Prompts.{resourceName}");
        using var streamReader = new StreamReader(resourceStream ?? throw new Exception($"Invalid {resourceName} stream"));
        return streamReader.ReadToEndAsync();
    }

    public static IEmbedding GetSelectedEmbedding(this IEnumerable<IEmbedding> embeddings)
    {
        var embeddingType = Environment.GetEnvironmentVariable("EmbeddingType") ?? throw new Exception("Missing EmbeddingType!");
        return embeddings.Single(x => x.GetType().Name == embeddingType);
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
}