using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace CustomServer.Tools;

public class HelloTool(ILogger<HelloTool> logger)
{
    [Function(nameof(SayHello))]
    public string SayHello(
        [McpToolTrigger(nameof(SayHello), "Responds to the user with a hello message.")] ToolInvocationContext context,
        [McpToolProperty(nameof(name), "string", "The name of the person to greet.")] string? name
    )
    {
        logger.LogInformation("C# MCP tool trigger function processed a request.");
        var entityToGreet = context?.Arguments?.GetValueOrDefault("name") ?? "world";
        return $"Hello, {entityToGreet}! This is an MCP Tool!";
    }
}
