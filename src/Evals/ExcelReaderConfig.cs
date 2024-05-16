using System.Text.Json;

namespace AIOChatbot.Evals;

public class ExcelReaderConfig
{
    public ExcelReaderConfig(IDictionary<string, object> config)
    {
        FilePath = ((JsonElement)config["FilePath"]).GetString() ?? throw new Exception("missing FilePath");
        QuestionColumn = ((JsonElement)config["QuestionColumn"]).GetString() ?? throw new Exception("missing QuestionColumn");
        StartRowIndex = ((JsonElement)config["StartRowIndex"]).GetInt32();
        AnswersColumn = ((JsonElement)config["AnswersColumn"]).Deserialize<string[]>() ?? throw new Exception("missing AnswersColumn");

        if (config.TryGetValue("CitationsColumn", out object? value0) && value0 is JsonElement citationsColumn)
        {
            CitationsColumn = citationsColumn.GetString();
        }

        if (config.TryGetValue("GroupIdColumn", out object? value1) && value1 is JsonElement groupIdColumn)
        {
            GroupIdColumn = groupIdColumn.GetString();
        }

        if (config.TryGetValue("IntentColumn", out object? value2) && value2 is JsonElement intentColumn)
        {
            IntentColumn = intentColumn.GetString();
        }

        WorkSheetIndex = ((JsonElement)config["WorkSheetIndex"]).GetInt32();
    }

    public string? GroupIdColumn { get; private set; }
    public string FilePath { get; private set; }
    public string QuestionColumn { get; private set; }
    public string? IntentColumn { get; private set; }
    public int StartRowIndex { get; private set; }
    public string[] AnswersColumn { get; private set; }
    public string? CitationsColumn { get; private set; }
    public int WorkSheetIndex { get; private set; }
}
