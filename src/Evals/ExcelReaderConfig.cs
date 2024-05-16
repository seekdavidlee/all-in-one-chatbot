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
        var citationsColumn = config["CitationsColumn"];
        if (citationsColumn is not null)
        {
            CitationsColumn = ((JsonElement)citationsColumn).GetString();
        }

        var groupIdColumn = config["GroupIdColumn"];
        if (groupIdColumn is not null)
        {
            GroupIdColumn = ((JsonElement)groupIdColumn).GetString();
        }

        var intentColumn = config["IntentColumn"];
        if (intentColumn is not null)
        {
            IntentColumn = ((JsonElement)intentColumn).GetString();
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
