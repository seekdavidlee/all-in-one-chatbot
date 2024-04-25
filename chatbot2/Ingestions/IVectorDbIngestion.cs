namespace chatbot2.Ingestions;

public interface IVectorDbIngestion
{
    Task RunAsync(IVectorDb vectorDb);
}
