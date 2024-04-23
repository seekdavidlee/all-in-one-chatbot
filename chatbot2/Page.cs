using HtmlAgilityPack;

namespace chatbot2;

public class Page
{
    private readonly HtmlNode node;
    private readonly PageContext pageContext;
    private readonly List<PageSection> sections = new();
    public Page(HtmlNode node, PageContext pageContext)
    {
        this.node = node;
        this.pageContext = pageContext;
    }

    public void Process()
    {
        var headers = node.SelectNodes("//h1");
        foreach (var header in headers)
        {
            foreach (var subHeader in header.SelectNodes("//h2"))
            {
                var section = new PageSection(subHeader, sections.Count, pageContext, $"# {header.InnerText}", "h2");
                section.Process();
                sections.Add(section);
            }
        }
    }

    public PageContext Context { get { return pageContext; } }

    public List<PageSection> Sections { get { return sections; } }
}
