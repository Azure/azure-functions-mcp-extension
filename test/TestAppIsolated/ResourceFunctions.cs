using System.Text.Json.Nodes;
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
                "version": 1.0,
                "releaseDate": "2024-01-01"
            },
            "test": {
                "example": ["list", "of", "values"]
            }
        }
        """;

    public ResourceFunctions(ILogger<TestFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(GetTextResource))]
    public FileResourceContents GetTextResource(
        [McpResourceTrigger(
            "file://readme.md",
            "readme",
            Title = "Application Readme",
            Description = "Application readme file",
            MimeType = "text/plain")]
        [McpMetadata(ReadmeMetadata)]
        ResourceInvocationContext context)
    {
        _logger.LogInformation("Reading text resource from local file storage");
        return new FileResourceContents
        {
            Path = Path.Combine(AppContext.BaseDirectory, "assets", "readme.md"),
            Meta = new JsonObject
            {
                ["contentKind"] = "documentation",
                ["sampleApp"] = "TestAppIsolated",
                ["render"] = new JsonObject
                {
                    ["mode"] = "markdown",
                    ["lineNumbers"] = false
                }
            }
        };
    }

    [Function(nameof(GetImageResource))]
    public FileResourceContents GetImageResource(
        [McpResourceTrigger(
            "file://logo.png",
            "logo",
            Description = "Azure Functions logo",
            MimeType = "image/png")] ResourceInvocationContext context)
    {
        _logger.LogInformation("Reading image from local file system");
        return new FileResourceContents
        {
            Path = Path.Combine(AppContext.BaseDirectory, "assets", "logo.png"),
            Meta = new JsonObject
            {
                ["contentKind"] = "image",
                ["sampleApp"] = "TestAppIsolated",
                ["dimensions"] = new JsonObject
                {
                    ["width"] = 256,
                    ["height"] = 256
                }
            }
        };
    }
}
