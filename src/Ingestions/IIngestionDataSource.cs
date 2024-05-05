namespace chatbot2.Ingestions;

public interface IIngestionDataSource
{
    Task<List<SearchModelDto>> LoadDataAsync(CancellationToken cancellationToken);
}
