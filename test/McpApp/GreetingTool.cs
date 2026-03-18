using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated;

public class GreetingTool
{
    private readonly ILogger<GreetingTool> _logger;

    public GreetingTool(ILogger<GreetingTool> logger)
    {
        _logger = logger;
    }

    [Function(nameof(GreetingTool))]
    public string Run(
        [McpToolTrigger("GreetingTool", "Greets the user with a customizable message.")]
        ToolInvocationContext context, string name)
    {
        return $"Hello, {name}!";
    }
}
