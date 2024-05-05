using System.Text.Json;

namespace AIOChatbot.Ingestions;

public static class Extensions
{
    public static (string Key, string? Value)? GetValue(this IDictionary<string, object> record, string path)
    {
        if (path.Contains('.'))
        {
            var levels = path.Split('.');
            var current = record;
            for (var i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                if (current is not null && current.TryGetValue(level, out object? levelObj) && levelObj is not null && levelObj is JsonElement levelElement)
                {
                    if (i == levels.Length - 1)
                    {
                        return (path.Replace(".", "__"), levelElement.GetStringValue());
                    }
                    current = levelElement.Deserialize<IDictionary<string, object>>();
                }
                else
                {
                    return (path.Replace(".", "__"), default);
                }
            }
        }
        else
        {
            if (record.TryGetValue(path, out object? obj) && obj is not null && obj is JsonElement element)
            {
                return (path, element.GetStringValue());
            }
        }
        return (path, default);
    }

    public static string? GetStringValue(this JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.Deserialize<int>().ToString();
        }
        return element.Deserialize<string>();
    }
}
