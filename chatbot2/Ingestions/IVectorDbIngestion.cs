namespace chatbot2.Ingestions;

public interface IVectorDbIngestion
{
    Task<List<SearchModelDto>> LoadDataAsync(CancellationToken cancellationToken);
}
