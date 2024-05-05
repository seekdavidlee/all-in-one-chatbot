namespace AIOChatbot.Ingestions;

public interface IIngestionProcessor
{
    Task ProcessAsync(List<SearchModelDto> searchModels, string collectionName, CancellationToken cancellationToken);
}
