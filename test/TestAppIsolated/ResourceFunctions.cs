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
        [McpResourceTrigger(
            "file:///resources/readme.txt",
            "ReadMe",
            Description = "Application readme file",
            MimeType = "text/plain")] ResourceInvocationContext context,
        [BlobInput("resources/readme.txt")] string blobContent)
    {
        _logger.LogInformation("Reading text resource from blob storage");  
        return blobContent;
    }

    [Function(nameof(GetBlobResource))]
    public byte[] GetBlobResource(
        [McpResourceTrigger(
            "file:///resources/logo.png",
            "Logo",
            Description = "Application logo file",
            MimeType = "image/png")]
        [McpResourceMetadata(
            "Author",
            "John Doe")]
        [McpResourceMetadata(
            "FileInfo",
            "{ \"file_version\" : \"1.0.0\", \"release_date\" : \"2024-01-01\" }")] ResourceInvocationContext context,
        [BlobInput("resources/logo.png")] byte[] blobContent)
    {
        _logger.LogInformation("Reading blob resource from blob storage");  
        return blobContent;
    }

    [Function(nameof(GetFunctionsLogo))] 
    public string GetFunctionsLogo(
        [McpToolTrigger(
            nameof(GetFunctionsLogo),
            "Returns the Azure Functions logo image resource.")]
        ToolInvocationContext context)
    {
        _logger.LogInformation("Returning logo blob resource");

        return "Testing image resource retrieval via MCP Tool Trigger.";
    }
}