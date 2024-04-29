using System.Collections.Concurrent;

namespace chatbot2;

public class FileCache
{
    private readonly ConcurrentDictionary<string, string> fileContents = new();

    public async Task<string> GetFileContentAsync(string filePath)
    {
        if (fileContents.TryGetValue(filePath, out var content))
        {
            return content;
        }

        var fileContent = await File.ReadAllTextAsync(filePath);
        fileContents.TryAdd(filePath, fileContent);
        return fileContent;
    }
}
