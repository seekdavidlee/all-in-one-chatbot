using ClosedXML.Excel;

namespace chatbot2.Evals;

public class ExcelGrouthTruthReader : IGroundTruthReader
{
    public string Name => "ExcelGrouthTruthReader";

    public Task<IEnumerable<GroundTruth>> ReadAsync(GroundTruthMapping groundTruthMapping)
    {
        if (groundTruthMapping.ReaderConfig is null)
        {
            return Task.FromResult(Enumerable.Empty<GroundTruth>());
        }

        List<GroundTruth> groundTruths = [];
        var config = new ExcelReaderConfig(groundTruthMapping.ReaderConfig);
        var workbook = new XLWorkbook(config.FilePath);
        var doc = workbook.Worksheets.ElementAt(config.WorkSheetIndex);

        var info = new FileInfo(config.FilePath);

        foreach (var row in doc.RowsUsed().Skip(config.StartRowIndex + 1))
        {
            var qns = row.Cell(config.QuestionColumn).GetString();
            if (string.IsNullOrEmpty(qns))
            {
                continue;
            }

            foreach (var answerCol in config.AnswersColumn)
            {
                string pointer = $"{row.RowNumber()},{answerCol}";
                var ans = row.Cell(answerCol).GetString();
                if (string.IsNullOrEmpty(ans))
                {
                    continue;
                }

                var groundTruth = new GroundTruth
                {
                    Question = qns,
                    Answer = ans,
                    DataSource = $"excel:{info.Name}",
                    EntrySource = pointer,
                };
                groundTruths.Add(groundTruth);
            }
        }
        return Task.FromResult(groundTruths.AsEnumerable());
    }
}
