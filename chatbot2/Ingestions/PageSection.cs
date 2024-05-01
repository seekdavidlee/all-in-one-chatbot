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

        List<string> h3Contents = [];

        StringBuilder sb = new();
        if (reference is not null)
        {
            sb.AppendLine(reference);
        }

        bool firstStopTag = true;
        string? h3 = null;
        StringBuilder h3Sb = new();
        while (true)
        {
            if (currrent is not null)
            {
                if (currrent.Name == stopTag && !firstStopTag)
                {
                    break;
                }

                if (currrent.Name == "h3")
                {
                    if (h3 is not null)
                    {
                        StringBuilder curh3Sb = new();
                        curh3Sb.AppendLine(reference);
                        curh3Sb.AppendLine(converter.Convert(currrent.OuterHtml));
                        curh3Sb.AppendLine(h3Sb.ToString());
                        h3Contents.Add(curh3Sb.ToString());
                        h3Sb.Clear();
                    }
                    h3 = currrent.InnerText;
                }

                if (h3 is not null)
                {
                    h3Sb.AppendLine(converter.Convert(converter.Convert(currrent.OuterHtml)));
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

        if (h3Sb.Length > 0)
        {
            h3Contents.Add(h3Sb.ToString());
        }

        if (h3Contents.Count > 1)
        {
            foreach (var content in h3Contents)
            {
                var id = $"{idPrefix}/{textChunks.Count}";
                var textChunk = new TextChunk(id, content);

                if (pageContext.PagePath is not null)
                {
                    textChunk.MetaDatas.Add("PAGE_PATH", pageContext.PagePath);
                }

                textChunks.Add(textChunk);
            }
        }
        else
        {
            var id = $"{idPrefix}/{textChunks.Count}";
            var textChunk = new TextChunk(id, sb.ToString());

            if (pageContext.PagePath is not null)
            {
                textChunk.MetaDatas.Add("PAGE_PATH", pageContext.PagePath);
            }

            textChunks.Add(textChunk);
        }
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