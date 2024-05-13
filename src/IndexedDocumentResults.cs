using AIOChatbot.VectorDbs;

namespace AIOChatbot;

public class IndexedDocumentResults
{
    public IndexedDocumentResults()
    {
        Documents = [];
    }
    public int TotalTokens { get; set; }
    public List<IndexedDocument> Documents { get; set; }
}