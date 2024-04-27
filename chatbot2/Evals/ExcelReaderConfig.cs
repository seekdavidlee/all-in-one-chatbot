using System.Text.Json;

namespace chatbot2.Evals;

public class ExcelReaderConfig
{
    public ExcelReaderConfig(IDictionary<string, object> config)
    {
        FilePath = ((JsonElement)config["FilePath"]).GetString() ?? throw new Exception("missing FilePath");
        QuestionColumn = ((JsonElement)config["QuestionColumn"]).GetString() ?? throw new Exception("missing QuestionColumn");
        StartRowIndex = ((JsonElement)config["StartRowIndex"]).GetInt32();
        AnswersColumn = ((JsonElement)config["AnswersColumn"]).Deserialize<string[]>() ?? throw new Exception("missing AnswersColumn");
        WorkSheetIndex = ((JsonElement)config["WorkSheetIndex"]).GetInt32();
    }

    public string FilePath { get; private set; }
    public string QuestionColumn { get; private set; }
    public int StartRowIndex { get; private set; }
    public string[] AnswersColumn { get; private set; }
    public int WorkSheetIndex { get; private set; }
}
