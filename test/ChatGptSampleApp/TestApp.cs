using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace TestAppIsolated;

public class TestApp
{
    private readonly ILogger<TestApp> _logger;

    public TestApp(ILogger<TestApp> logger)
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
    public WelcomeResult GetWelcomeMessage(
        [McpToolTrigger(nameof(GetWelcomeMessage))]
        ToolInvocationContext context,
        [McpToolProperty(nameof(argument), "The name of the person to greet.")] 
        string argument)
    {
        _logger.LogInformation("Returning welcome page resource link");

        return new WelcomeResult { Message = $"Hello {argument}, and welcome to the MCP Functions Test App!" };
    }

    [McpResult]
    public class WelcomeResult
    {
        public string Message { get; set; }
    }
}
