using AIOChatbot.Evals;
using Microsoft.Extensions.DependencyInjection;

namespace AIOChatbot.Commands;

public static class Extensions
{
    public static void AddImportGroundTruthsCommand(this ServiceCollection services)
    {
        services.AddSingleton<GroundTruthIngestion>();
        services.AddSingleton<ICommandAction, ImportGroundTruthsCommand>();
        services.AddSingleton<IGroundTruthReader, ExcelGrouthTruthReader>();
    }
}
