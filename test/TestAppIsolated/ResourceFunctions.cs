using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated;

public class ResourceFunctions
{
    private readonly ILogger<TestFunction> _logger;

    private const string ReadmeMetadata = """
        {
            "author": "John Doe",
            "file": {
                "version": "1.0.0",
                "releaseDate": "2024-01-01"
            },
            "tags": ["documentation", "readme"]
        }
        """;

    public ResourceFunctions(ILogger<TestFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(GetTextResource))]
    public string GetTextResource(
        [McpResourceTrigger(
            "file://readme.md",
            "readme",
            Description = "Application readme file",
            MimeType = "text/plain")]
        [McpMetadata(ReadmeMetadata)]
        ResourceInvocationContext context)
    {
        _logger.LogInformation("Reading text resource from local file storage");
        var file = Path.Combine(AppContext.BaseDirectory, "assets", "readme.md");
        return File.ReadAllText(file);
    }

    [Function(nameof(GetImageResource))]
    public byte[] GetImageResource(
        [McpResourceTrigger(
            "file://logo.png",
            "logo",
            Description = "Azure Functions logo",
            MimeType = "image/png")] ResourceInvocationContext context)
    {
        _logger.LogInformation("Reading image from local file system");
        var filePath = Path.Combine(AppContext.BaseDirectory, "assets", "logo.png");
        return File.ReadAllBytes(filePath);
    }
}
