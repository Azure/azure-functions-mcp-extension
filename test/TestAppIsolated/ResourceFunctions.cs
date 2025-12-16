using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated;

public class ResourceFunctions
{
    private readonly ILogger<ResourceFunctions> _logger;

    public ResourceFunctions(ILogger<ResourceFunctions> logger)
    {
        _logger = logger;
    }

    [Function(nameof(GetTextResource))]
    public string GetTextResource(
        [McpResourceTrigger("file://resources/readme.txt", "ReadMe", Description = "Application readme file", MimeType = "text/plain")]
        [McpResourceMetadata("Author", "John Doe")]
        [McpResourceMetadata("FileInfo", "{ \"file_version\" : \"1.0.0\", \"release_date\" : \"2024-01-01\" }")]
        ResourceInvocationContext context)
    {
        _logger.LogInformation("Reading text resource from resources/readme.txt");  
    
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "resources", "readme.txt");
        return File.ReadAllText(filePath);
    }

    [Function(nameof(GetBlobResource))]
    public byte[] GetBlobResource(
        [McpResourceTrigger("file://resources/logo.png", "Logo", Description = "Application logo file", MimeType = "image/png")]
        [McpResourceMetadata("Author", "John Doe")]
        [McpResourceMetadata("FileInfo", "{ \"file_version\" : \"1.0.0\", \"release_date\" : \"2024-01-01\" }")]
        ResourceInvocationContext context)
    {
        _logger.LogInformation("Reading blob resource from resources/logo.png");  
    
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "resources", "logo.png");
        return File.ReadAllBytes(filePath);
    }
}