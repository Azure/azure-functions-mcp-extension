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
            Title = "Application Readme",
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

    /// <summary>
    /// Example using FileResourceContents for text files.
    /// The framework reads the file and creates TextResourceContents automatically.
    /// Relative paths are resolved against AppContext.BaseDirectory.
    /// </summary>
    [Function(nameof(GetTextResourceFromFile))]
    public FileResourceContents GetTextResourceFromFile(
        [McpResourceTrigger(
            "file://readme-v2.md",
            "readme-v2",
            Title = "Readme (FileResourceContents)",
            Description = "Application readme served via FileResourceContents",
            MimeType = "text/plain")]
        ResourceInvocationContext context)
    {
        _logger.LogInformation("Serving text resource via FileResourceContents");
        return new FileResourceContents
        {
            Path = Path.Combine("assets", "readme.md")
        };
    }

    /// <summary>
    /// Example using FileResourceContents for binary files.
    /// The framework reads the file and creates BlobResourceContents (base64) automatically,
    /// using the MimeType from the trigger attribute to determine binary vs text encoding.
    /// </summary>
    [Function(nameof(GetImageResourceFromFile))]
    public FileResourceContents GetImageResourceFromFile(
        [McpResourceTrigger(
            "file://logo-v2.png",
            "logo-v2",
            Description = "Azure Functions logo served via FileResourceContents",
            MimeType = "image/png")]
        ResourceInvocationContext context)
    {
        _logger.LogInformation("Serving image resource via FileResourceContents");
        return new FileResourceContents
        {
            Path = "assets/logo.png"
        };
    }

    [Function(nameof(UserProfileResourceTemplate))]
    public FileResourceContents UserProfileResourceTemplate(
        [McpResourceTrigger(
            "user://profile/{name}",
            "userProfile",
            Description = "User profile resource",
            MimeType = "application/json")] ResourceInvocationContext context, string name)
    {
        _logger.LogInformation("Reading user profile template for {Name}", name);
        return new FileResourceContents
        {
            Path = Path.Combine("assets", $"{name}.md")
        };
    }

    [Function(nameof(CatalogItemResource))]
    public string CatalogItemResource(
        [McpResourceTrigger(
            "store://catalog/{category}items{tag}",
            "catalogItem",
            Description = "Catalog item lookup by category and tag",
            MimeType = "application/json")] ResourceInvocationContext context, string category, string tag)
    {
        _logger.LogInformation("Looking up catalog item: category={Category}, tag={Tag}", category, tag);
        return $"{{\"category\":\"{category}\",\"tag\":\"{tag}\"}}";
    }
}
