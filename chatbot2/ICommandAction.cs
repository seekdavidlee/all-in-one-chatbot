using Microsoft.Extensions.Configuration;

namespace chatbot2;

public interface ICommandAction
{
    string Name { get; }
    Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken);
}
