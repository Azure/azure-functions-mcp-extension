using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpListener : IListener
{
    public McpListener(ITriggeredFunctionExecutor executor, IMcpRequestHandler requestHandler)
    {
        ArgumentNullException.ThrowIfNull(executor);

        Executor = executor;
    }

    public ITriggeredFunctionExecutor Executor { get; }

    public void Dispose() { }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Cancel() { }
}