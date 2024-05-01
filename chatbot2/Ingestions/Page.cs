using HtmlAgilityPack;

namespace chatbot2.Ingestions;

public class Page
{
    private readonly HtmlNode node;
    private readonly PageContext pageContext;
    private readonly List<PageSection> sections = [];
    private readonly List<PageLogEntry> logs;
    public Page(HtmlNode node, PageContext pageContext, List<PageLogEntry> logs)
    {
        this.node = node;
        this.pageContext = pageContext;
        this.logs = logs;
    }

    public void Process()
    {
        HtmlNodeCollection headers;
        try
        {
            headers = node.SelectNodes("//h1");
            if (headers is null)
            {
                logs.Add(new PageLogEntry { Source = pageContext.PagePath, Text = $"h1 not found: {node.OuterHtml}" });
                return;
            }
        }
        catch (NullReferenceException)
        {
            logs.Add(new PageLogEntry { Source = pageContext.PagePath, Text = $"error processing h2: {node.OuterHtml}" });
            return;
        }

        foreach (var header in headers)
        {
            try
            {
                foreach (var subHeader in header.SelectNodes("//h2"))
                {
                    var section = new PageSection(subHeader, sections.Count, pageContext, $"# {header.InnerText}", "h2");
                    section.Process();
                    sections.Add(section);
                }
            }
            catch (NullReferenceException)
            {
                logs.Add(new PageLogEntry { Source = pageContext.PagePath, Text = $"error processing h2: {header.OuterHtml}" });
            }
        }
    }

    public PageContext Context { get { return pageContext; } }

    public List<PageSection> Sections { get { return sections; } }
}
