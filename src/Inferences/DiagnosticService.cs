using System.Diagnostics;

namespace AIOChatbot.Inferences;

public static class DiagnosticService
{
    const string SourceName = nameof(Inferences);
    public static readonly ActivitySource Source = new(SourceName);
}
