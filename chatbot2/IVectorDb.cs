using chatbot2.VectorDbs;

namespace chatbot2;

public interface IVectorDb
{
    Task InitAsync();
    Task ProcessAsync(PageSection pageSection);
    Task DeleteAsync();
    Task<IEnumerable<IndexedDocument>> SearchAsync(string searchText);
}
