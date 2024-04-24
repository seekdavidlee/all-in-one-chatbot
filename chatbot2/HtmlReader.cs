using HtmlAgilityPack;

namespace chatbot2;

public class HtmlReader
{
    private readonly string pageContentXPath;
    public HtmlReader()
    {
        pageContentXPath = Environment.GetEnvironmentVariable("PageContentXPath") ?? throw new Exception("Missing PageContentXPath");
    }
    public async Task<(List<Page> Pages, List<PageLogEntry> Logs)> ReadFilesAsync(string sourceDirectory)
    {
        var logs = new List<PageLogEntry>();
        List<Page> pages = [];
        await InternalReadFilesAsync(sourceDirectory, pages, logs);
        return (pages, logs);
    }

    private async Task InternalReadFilesAsync(string sourceDirectory, List<Page> pages, List<PageLogEntry> logs)
    {
        var dirs = Directory.GetDirectories(sourceDirectory);
        foreach (var dir in dirs)
        {
            await InternalReadFilesAsync(dir, pages, logs);
        }

        foreach (var filePath in Directory.GetFiles(sourceDirectory, "*.htm"))
        {
            var page = await ReadFileAsync(filePath, logs);
            if (page is null)
            {
                continue;
            }
            pages.Add(page);
        };
    }

    public async Task<Page?> ReadFileAsync(string filePath, List<PageLogEntry> logs)
    {
        using var reader = new StreamReader(filePath);

        var doc = new HtmlDocument();
        doc.LoadHtml(await reader.ReadToEndAsync());

        var node = doc.DocumentNode.SelectSingleNode(pageContentXPath);
        if (node is null)
        {
            logs.Add(new PageLogEntry { Source = filePath, Text = $"{pageContentXPath} not found" });
            return default;
        }
        Page page = new(node, new PageContext { PagePath = filePath }, logs);
        page.Process();
        return page;
    }
}
