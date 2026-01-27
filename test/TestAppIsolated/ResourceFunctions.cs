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

    [Function(nameof(GetTextResource))]
    public string GetTextResource(
    [McpResourceTrigger(
        "file://resources/readme.txt",
        "ReadMe",
        Description = "Application readme file",
        MimeType = "text/plain")] ResourceInvocationContext context)
    {
        _logger.LogInformation("Reading text resource from local file storage");
        var filePath = Path.Combine(AppContext.BaseDirectory, "resources", "readme.txt");
        return File.ReadAllText(filePath);
    }

    [Function(nameof(GetImageResource))]
    public byte[] GetImageResource(
        [McpResourceTrigger(
        "file://icon.png",
        "Icon",
        Description = "Azure Functions logo",
        MimeType = "image/png")] ResourceInvocationContext context)
    {
        _logger.LogInformation("Reading image from local file system");
        var filePath = Path.Combine(AppContext.BaseDirectory, "icon.png");
        return File.ReadAllBytes(filePath);
    }
}
