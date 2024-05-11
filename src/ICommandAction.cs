using Microsoft.Extensions.Configuration;

namespace AIOChatbot;

public interface ICommandAction
{
    string Name { get; }
    bool LongRunning { get; }
    Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken);
}
