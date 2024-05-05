namespace AIOChatbot.Ingestions;

public interface IIngestionDataSource
{
    Task<List<SearchModelDto>> LoadDataAsync(CancellationToken cancellationToken);
}
