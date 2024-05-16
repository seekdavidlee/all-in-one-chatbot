using ClosedXML.Excel;

namespace AIOChatbot.Evals;

public class ExcelGrouthTruthReader : IGroundTruthReader
{
    public string Name => "ExcelGrouthTruthReader";

    public Task<IEnumerable<GroundTruthGroup>> ReadAsync(GroundTruthMapping groundTruthMapping)
    {
        if (groundTruthMapping.ReaderConfig is null)
        {
            return Task.FromResult(Enumerable.Empty<GroundTruthGroup>());
        }

        Dictionary<string, GroundTruthGroup> groundTruths = [];
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

            string? intent = row.Cell(config.IntentColumn).GetString();

            foreach (var answerCol in config.AnswersColumn)
            {
                string pointer = $"{row.RowNumber()},{answerCol}";
                var ans = row.Cell(answerCol).GetString();
                if (string.IsNullOrEmpty(ans))
                {
                    continue;
                }

                List<GroundTruthCitation> citations = [];
                if (config.CitationsColumn is not null)
                {
                    var citationsCell = row.Cell(config.CitationsColumn).GetString();
                    if (citationsCell is not null)
                    {
                        citationsCell.Split(';').ToList().ForEach(citationLine =>
                        {
                            var parts = citationLine.Split(',');
                            if (parts.Length == 3)
                            {
                                citations.Add(new GroundTruthCitation
                                {
                                    Key = parts[0],
                                    Title = parts[1],
                                    Source = parts[2],
                                });
                            }
                        });
                    }
                }

                string groupId;
                if (config.GroupIdColumn is not null)
                {
                    var groupIdCell = row.Cell(config.CitationsColumn).GetString();
                    groupId = groupIdCell ?? Guid.NewGuid().ToString("N");
                }
                else
                {
                    groupId = Guid.NewGuid().ToString("N");
                }

                GroundTruthGroup group;
                if (!groundTruths.TryGetValue(groupId, out var tGroup))
                {
                    group = new GroundTruthGroup();
                    groundTruths.Add(groupId, group);
                }
                else
                {
                    group = tGroup;
                }

                group.GroundTruths.Add(new GroundTruth
                {
                    Question = qns,
                    Answer = ans,
                    DataSource = $"excel:{info.Name}",
                    EntrySource = pointer,
                    Citations = citations,
                    Intent = intent,
                });
            }
        }
        return Task.FromResult(groundTruths.Select(x => x.Value).AsEnumerable());
    }
}
