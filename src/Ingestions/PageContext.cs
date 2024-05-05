namespace chatbot2.Ingestions;

public class PageContext
{
    public string? PagePath { get; set; }

    public string GetFileName(string removePrefix)
    {
        if (PagePath is null)
        {
            return string.Empty;
        }

        return PagePath.Replace(removePrefix, string.Empty)
            .Replace(".htm", string.Empty)
            .Replace(".html", string.Empty);
    }
}
