using System.Text;

namespace chatbot2.VectorDbs;

public class IndexedDocument
{
    public string? Id { get; set; }
    public float? Score { get; set; }
    public IDictionary<string, string>? MetaDatas { get; set; }

    public string? Text { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Id: {Id}");
        sb.AppendLine($"Score: {Score}");

        if (MetaDatas is not null)
        {
            sb.AppendLine("MetaData:");
            foreach (var m in MetaDatas)
            {
                sb.AppendLine($"{m.Key}: {m.Value}");
            }
        }

        if (Text is not null)
        {
            sb.AppendLine($"DocumentText: {Text}");
        }

        return sb.ToString();
    }
}
