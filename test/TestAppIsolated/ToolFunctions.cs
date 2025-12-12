using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using static TestAppIsolated.ToolsInformation;

namespace TestAppIsolated;

public class TestFunction
{
    private readonly ILogger<TestFunction> _logger;

    public TestFunction(ILogger<TestFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(GetFunctionsLogo))]
    public ResourceLinkBlock GetFunctionsLogo(
        [McpToolTrigger("GetFunctionsLogo", "Returns the Azure Functions logo as a base64-encoded string.")]
        [McpMetadata("""{"version": 1.0, "author": "Jane Doe"}""")]
        ToolInvocationContext context)
    {
        return new ResourceLinkBlock
        {
            Uri = "file://logo.png",
            Name = "Azure Functions Logo",
            MimeType = "image/png"
        };
    }

    [Function(nameof(HappyFunction))]
    public string HappyFunction(
        [McpToolTrigger(nameof(HappyFunction), "Responds to the user with a hello message.")] ToolInvocationContext context,
        [McpToolProperty(nameof(name), "The name of the person to greet.")] string name,
        [McpToolProperty(nameof(job), "The job of the person.")] JobType? job,
        [McpToolProperty("attributes", "Attributes of the person.")] IEnumerable<string> attributes,
        [McpToolProperty("numbers", "Attributes of the person.")] IEnumerable<int> numbers,
        [McpToolProperty(nameof(age), "The age of the person.")] int age = 20,
        [McpToolProperty(nameof(isHappy), "The happiness of the person.")] bool isHappy = true)
    {
        _logger.LogInformation("C# MCP tool trigger function processed a request: {ToolName}", context.Name);
        var entityToGreet = name ?? "world";
        return $"""
            Hello, {entityToGreet}! Job: {job ?? JobType.Unemployed} | Age: {age} | Happy? {isHappy}.
            Attributes: {(attributes is ICollection { Count: > 0} ? string.Join(", ", attributes) : "(none)")}
            """;
    }
    */

    [Function(nameof(MultiContentTypeFunction))]
    public IList<ContentBlock> MultiContentTypeFunction(
        [McpToolTrigger(nameof(MultiContentTypeFunction), "Responds to user with multiple content blocks.")] ToolInvocationContext context,
        [McpToolProperty(nameof(data), "Base64-encoded image data", true)] string data,
        [McpToolProperty(nameof(mimeType), "Mime type", false)] string? mimeType)
    {
        return new List<ContentBlock>
        {
            new TextContentBlock { Text = "Here is an image for you!" },
            new ResourceLinkBlock { Name = "example", Uri = "https://www.google.com/", Description = "Image Information" },
            new ImageContentBlock { Data = data, MimeType = mimeType ?? "image/jpeg" }
        };
    }

    [Function(nameof(RenderImage))]
    public ImageContentBlock RenderImage(
        [McpToolTrigger(nameof(RenderImage), "Responds to user with an image.")] ToolInvocationContext context,
        [McpToolProperty(nameof(data), "Base64-encoded image data", true)] string data,
        [McpToolProperty(nameof(mimeType), "Mime type", false)] string? mimeType)
    {
        return new ImageContentBlock { Data = data, MimeType = mimeType ?? "image/jpeg" };
    }

    [Function(nameof(BirthdayTracker))]
    public string BirthdayTracker(
        [McpToolTrigger(nameof(BirthdayTracker), "Capture user birthday information.")] ToolInvocationContext context,
        [McpToolProperty(nameof(userId), "User ID")] Guid userId,
        [McpToolProperty(nameof(birthday), "Birthday")] DateTime birthday)
    {
        var date = birthday.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
        return $"Hello {userId}, {date}";
    }

    /*
    [Function(nameof(SingleArgumentFunction))]
    public string SingleArgumentFunction(
       [McpToolTrigger(nameof(SingleArgumentFunction), "Echoes a single argument passed into the tool.")] ToolInvocationContext context,
       [McpToolProperty(nameof(argument), "The name of the person to greet.")] string argument)
    {
        _logger.LogInformation("C# MCP tool trigger function processed a request: {ToolName}", context.Name);
        return argument ?? "(null argument)";
    }

    [Function(nameof(SingleArgumentWithDefaultFunction))]
    public string SingleArgumentWithDefaultFunction(
       [McpToolTrigger(nameof(SingleArgumentWithDefaultFunction), "Echoes a single argument passed into the tool.")] ToolInvocationContext context,
       [McpToolProperty(nameof(argument), "The name of the person to greet.")] string argument = "(no-argument)")
    {
        _logger.LogInformation("C# MCP tool trigger function processed a request: {ToolName}", context.Name);
        return argument;
    }

    [Function(nameof(GetSnippet))]
    public Snippet? GetSnippet(
        [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)] ToolInvocationContext context,
        [McpToolProperty(SnippetNamePropertyName, SnippetNamePropertyDescription, true)] string name)
    {
        return SnippetsCache.Snippets.TryGetValue(name, out var content)
            ? new Snippet { Name = name, Content = content }
            : null;
    }

    [Function(nameof(SaveSnippet))]
    public void SaveSnippet(
        [McpToolTrigger(SaveSnippetToolName, SaveSnippetToolDescription)] Snippet snippet,
        ToolInvocationContext context)
    {
        _logger.LogInformation($"Tool name: {context.Name}");
        SnippetsCache.Snippets[snippet.Name] = snippet.Content ?? string.Empty;
    }

    [Function(nameof(SearchSnippets))]
    public object SearchSnippets(
        [McpToolTrigger(SearchSnippetsToolName, SearchSnippetsToolDescription)] SnippetSearchRequest searchRequest,
        ToolInvocationContext context)
    {
        var comparisonType = searchRequest.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return SnippetsCache.Snippets
            .Where(kvp => kvp.Key.Contains(searchRequest.Pattern, comparisonType))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
    */

    [Function(nameof(GetImageWithMetadata))]
    public CallToolResult GetImageWithMetadata(
        [McpToolTrigger("GetImageWithMetadata", "Returns an image with metadata as structured content")] ToolInvocationContext context)
    {
        // Manually construct CallToolResult with explicit content blocks and structured content
        var metadata = new
        {
            ImageId = "logo",
            Format = "png",
            CreatedAt = DateTime.UtcNow,
            Tags = new[] { "functions" }
        };

        var metadataJson = JsonSerializer.Serialize(metadata);
        var imagePath = Path.Combine(AppContext.BaseDirectory, "assets", "logo.png");
        byte[] imageBytes = File.ReadAllBytes(imagePath);

        return new CallToolResult
        {
            Content = new List<ContentBlock> 
            { 
                // REQUIRED: TextContent block with serialized structured content (for backwards compatibility)
                new TextContentBlock { Text = metadataJson },
                new ImageContentBlock { Data = Convert.ToBase64String(imageBytes), MimeType = "image/png" }
            },
            // Structured content for clients that support it
            StructuredContent = JsonNode.Parse(metadataJson)
        };
    }

    /// <summary>
    /// Snippet class decorated with McpContent to enable structured content output.
    /// When returned from a tool function, this will be serialized as both text content
    /// (for backwards compatibility) and structured content (for clients that support it).
    /// </summary>
    [McpContent]
    public class Snippet
    {
        [Description("The name of the snippet")]
        public required string Name { get; set; }

        [Description("The content of the snippet")]
        public string? Content { get; set; }
    }

    public class SnippetSearchRequest
    {
        [Description("Pattern to search for")]
        public required string Pattern { get; set; }

        [Description("Whether search is case sensitive")]
        public bool CaseSensitive { get; set; }
    }


    public enum JobType
    {
        FullTime,
        PartTime,
        Contract,
        Internship,
        Temporary,
        Freelance,
        Unemployed
    }

}
