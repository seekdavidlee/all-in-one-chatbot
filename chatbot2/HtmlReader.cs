using HtmlAgilityPack;

namespace chatbot2;

public class HtmlReader
{
    public async Task<List<Page>> ReadFilesAsync(string sourceDirectory)
    {
        List<Page> pages = [];
        await InternalReadFilesAsync(sourceDirectory, pages);
        return pages;
    }

    private async Task InternalReadFilesAsync(string sourceDirectory, List<Page> pages)
    {
        var dirs = Directory.GetDirectories(sourceDirectory);
        foreach (var dir in dirs)
        {
            await InternalReadFilesAsync(dir, pages);
        }

        foreach (var filePath in Directory.GetFiles(sourceDirectory, "*.htm"))
        {
            pages.Add(await ReadFileAsync(filePath));
        };
    }

    public async Task<Page> ReadFileAsync(string filePath)
    {
        using var reader = new StreamReader(filePath);

        var doc = new HtmlDocument();
        doc.LoadHtml(await reader.ReadToEndAsync());

        var node = doc.DocumentNode.SelectSingleNode(Environment.GetEnvironmentVariable("PageContentXPath"));
        Page page = new(node, new PageContext { PagePath = filePath });
        page.Process();
        return page;
    }
}
