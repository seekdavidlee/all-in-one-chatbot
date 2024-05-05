using Microsoft.Extensions.Configuration;

namespace AIOChatbot.Commands;

public class DeleteSearchCommand : ICommandAction
{
    private readonly IEnumerable<IVectorDb> vectorDbs;
    public DeleteSearchCommand(IEnumerable<IVectorDb> vectorDbs)
    {
        this.vectorDbs = vectorDbs;
    }
    public string Name => "delete-search";

    public async Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken)
    {
        var vectorDb = vectorDbs.GetSelectedVectorDb();
        await vectorDb.DeleteAsync();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("database deleted");
        Console.ResetColor();
    }
}
