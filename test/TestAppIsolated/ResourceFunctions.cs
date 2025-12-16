using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated;

public class ResourceFunctions
{
    private readonly ILogger<TestFunction> _logger;

    public ResourceFunctions(ILogger<TestFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(GetWelcomeHtml))]
    public string GetWelcomeHtml(
        [McpResourceTrigger(
            "file://widget/welcome.html",
            "WelcomePage",
            Description = "A simple HTML welcome page",
            MimeType = "text/html+skybridge")] ResourceInvocationContext context)
    {
        _logger.LogInformation("Processing resource call from GetWelcomeHtml...");
        var file = Path.Combine(AppContext.BaseDirectory, "web", "welcome.html");
        return File.ReadAllText(file);
    }
}
