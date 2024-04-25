using Microsoft.Extensions.Configuration;

namespace chatbot2.Commands;

public class DeleteSearchCommand : ICommandAction
{
    private readonly IVectorDb vectorDb;
    public DeleteSearchCommand(IEnumerable<IVectorDb> vectorDbs)
    {
        vectorDb = vectorDbs.GetSelectedVectorDb();
    }
    public string Name => "delete-search";

    public async Task ExecuteAsync(IConfiguration argsConfiguration)
    {
        await vectorDb.DeleteAsync();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("database deleted");
        Console.ResetColor();
    }
}
