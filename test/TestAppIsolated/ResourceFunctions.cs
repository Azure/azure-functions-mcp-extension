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

    [Function(nameof(GetResourceUsingFileAbstraction))]
    public FileResourceContents GetResourceUsingFileAbstraction(
        [McpResourceTrigger(
            "file://welcome.html",
            "welcome",
            Description = "Welcome page using FileResourceContents abstraction",
            MimeType = "text/html")] ResourceInvocationContext context)
    {
        _logger.LogInformation("Reading resource using FileResourceContents abstraction");
        var filePath = Path.Combine(AppContext.BaseDirectory, "assets", "welcome.html");
        
        return new FileResourceContents
        {
            Path = filePath
        };
    }

    [Function(nameof(GetBinaryResourceUsingFileAbstraction))]
    public FileResourceContents GetBinaryResourceUsingFileAbstraction(
        [McpResourceTrigger(
            "file://banner.png",
            "banner",
            Description = "Banner image using FileResourceContents abstraction",
            MimeType = "image/png")] ResourceInvocationContext context)
    {
        _logger.LogInformation("Reading binary resource using FileResourceContents abstraction");
        var filePath = Path.Combine(AppContext.BaseDirectory, "assets", "banner.png");
        
        return new FileResourceContents
        {
            Path = filePath
        };
    }

    [Function(nameof(GetResourceWithMetadata))]
    public FileResourceContents GetResourceWithMetadata(
        [McpResourceTrigger(
            "file://config.json",
            "config",
            Description = "Configuration file with metadata",
            MimeType = "application/json")] ResourceInvocationContext context)
    {
        _logger.LogInformation("Reading resource with custom metadata");
        var filePath = Path.Combine(AppContext.BaseDirectory, "assets", "config.json");
        
        return new FileResourceContents
        {
            Path = filePath,
            Meta = new System.Text.Json.Nodes.JsonObject
            {
                ["version"] = "1.0",
                ["lastModified"] = DateTime.UtcNow.ToString("O"),
                ["environment"] = "development"
            }
        };
    }
}
