namespace chatbot2.Ingestions;

public interface IIngestionProcessor
{
    Task ProcessAsync(List<SearchModelDto> searchModels, CancellationToken cancellationToken);
}
