using HtmlAgilityPack;
using System.Text;

namespace chatbot2.Ingestions;
public class PageSection
{
    private readonly HtmlNode node;
    private readonly PageContext pageContext;
    private readonly string? reference;
    private readonly string stopTag;
    private readonly List<TextChunk> textChunks = [];
    private readonly string idPrefix;
    private readonly ReverseMarkdown.Converter converter = new();
    public PageSection(HtmlNode node, int sectionCounter, PageContext pageContext, string? reference, string stopTag)
    {
        this.node = node;
        this.pageContext = pageContext;
        this.reference = reference;
        this.stopTag = stopTag;

        var removePrefix = Environment.GetEnvironmentVariable("RemovePagePathPrefix");
        idPrefix = removePrefix is not null ? $"{pageContext.GetFileName(removePrefix)}/{sectionCounter}" : $"/{sectionCounter}";
    }

    public string IdPrefix { get { return idPrefix; } }

    public void Process()
    {
        var currrent = node;
        StringBuilder sb = new();
        if (reference is not null)
        {
            sb.AppendLine(reference);
        }

        bool firstStopTag = true;
        while (true)
        {
            if (currrent is not null)
            {
                if (currrent.Name == stopTag && !firstStopTag)
                {
                    break;
                }

                firstStopTag = false;

                sb.AppendLine(converter.Convert(currrent.OuterHtml));
                currrent = currrent.NextSibling;
            }
            else
            {
                break;
            }
        }

        var id = $"{idPrefix}/{textChunks.Count}";
        var textChunk = new TextChunk(id, sb.ToString());

        if (pageContext.PagePath is not null)
        {
            textChunk.MetaDatas.Add("PAGE_PATH", pageContext.PagePath);
        }

        textChunks.Add(textChunk);
    }

    public List<TextChunk> TextChunks { get { return textChunks; } }

    public override string ToString()
    {
        StringBuilder sb = new();
        foreach (var textChunk in textChunks)
        {
            sb.AppendLine($"START==chunk[{textChunk.Id}]==");
            foreach (var meta in textChunk.MetaDatas)
            {
                sb.AppendLine($"{meta.Key}='{meta.Value}'");
            }
            sb.AppendLine(textChunk.Text);
            sb.AppendLine($"END==chunk[{textChunk.Id}]==");
        }

        return sb.ToString();
    }
}