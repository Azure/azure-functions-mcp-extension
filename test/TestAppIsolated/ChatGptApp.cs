using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated;

public class ChatGptApp
{
    private readonly ILogger<ChatGptApp> _logger;

    public ChatGptApp(ILogger<ChatGptApp> logger)
    {
        _logger = logger;
    }

    [Function(nameof(GetWelcomeHtml))]
    public string GetWelcomeHtml(
        [McpResourceTrigger(
            "ui://widget/welcome.html",
            "Welcome Page",
            MimeType = "text/html+skybridge",
            Description = "A simple HTML welcome page")]
        [McpResourceMetadata("openai/widgetPrefersBorder", true)]
        [McpResourceMetadata("openai/widgetDomain", "https://chatgpt.com")]
        [McpResourceMetadata("openai/widgetCSP", "{\"connect_domains\":[],\"resource_domains\":[]}")]
        ResourceInvocationContext context)
    {
        var file = Path.Combine(AppContext.BaseDirectory, "resources", "welcome.html");
        return File.ReadAllText(file);
    }

    // Required metadata hardcoded in MCP host for testing purposes within `DefaultToolRegistry`
    // openai/outputTemplate = ui://widget/welcome.html
    [Function(nameof(GetWelcomeMessage))]
    public string GetWelcomeMessage(
        [McpToolTrigger(
            nameof(GetWelcomeMessage),
            "Returns a link to the welcome page HTML resource.")] ToolInvocationContext context)
    {
        _logger.LogInformation("Returning welcome page resource link");

        return "Hello, and welcome to the MCP Functions Test App!";
    }
}