using chatbot2.VectorDbs;

namespace chatbot2;

public interface IVectorDb
{
    Task InitAsync();
    Task ProcessAsync(IEnumerable<SearchModel> models);
    Task DeleteAsync();
    Task<IEnumerable<IndexedDocument>> SearchAsync(string searchText);
}
