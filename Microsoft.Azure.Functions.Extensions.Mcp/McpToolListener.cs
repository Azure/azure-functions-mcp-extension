using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpToolListener(ITriggeredFunctionExecutor executor, string functionName, string toolName, string? toolDescription = null)
    : IListener, IMcpTool
{
    public ITriggeredFunctionExecutor Executor { get; } = executor;

    public string FunctionName { get; } = functionName;

    string IMcpTool.Name { get; } = toolName;

    public string? Description { get; set; } = toolDescription;

    public void Dispose() { }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Cancel() { }

    public async Task<object> RunAsync(CancellationToken cancellationToken)
    {
        var input = new TriggeredFunctionData
        {
            TriggerValue = this
        };

        var result = await Executor.TryExecuteAsync(input, cancellationToken);

        // TODO: process result
        return string.Empty;
    }
}