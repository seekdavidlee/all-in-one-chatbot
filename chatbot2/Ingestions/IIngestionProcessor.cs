namespace chatbot2.Ingestions;

public interface IIngestionProcessor
{
    Task ProcessAsync(List<SearchModelDto> searchModels, string collectionName, CancellationToken cancellationToken);
}
