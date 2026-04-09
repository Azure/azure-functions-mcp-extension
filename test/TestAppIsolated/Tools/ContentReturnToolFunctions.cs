// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace TestAppIsolated.Tools;

/// <summary>
/// Tools that test different content return types: text, image, resource link,
/// multi-content blocks, and structured content via CallToolResult.
/// </summary>
public class ContentReturnToolFunctions(ILogger<ContentReturnToolFunctions> logger)
{
    /// <summary>
    /// Returns a plain string. Tests basic text content return.
    /// </summary>
    [Function(nameof(TextContentTool))]
    public string TextContentTool(
        [McpToolTrigger(nameof(TextContentTool), "Returns plain text content.")] ToolInvocationContext context)
    {
        logger.LogInformation("TextContentTool invoked");
        return "This is plain text content.";
    }

    /// <summary>
    /// Returns an ImageContentBlock. Tests binary/image content return.
    /// Example: { "data": "iVBORw0KGgoAAAANSUhEUg==", "mimeType": "image/png" }
    /// </summary>
    [Function(nameof(ImageContentTool))]
    public ImageContentBlock ImageContentTool(
        [McpToolTrigger(nameof(ImageContentTool), "Returns an image content block.")] ToolInvocationContext context,
        [McpToolProperty("data", "Base64-encoded image data.", true)] string data,
        [McpToolProperty("mimeType", "Image MIME type.", false)] string? mimeType)
    {
        logger.LogInformation("ImageContentTool invoked");
        return new ImageContentBlock { Data = Encoding.UTF8.GetBytes(data), MimeType = mimeType ?? "image/jpeg" };
    }

    /// <summary>
    /// Returns a ResourceLinkBlock. Tests resource link content return.
    /// </summary>
    [Function(nameof(ResourceLinkTool))]
    public ResourceLinkBlock ResourceLinkTool(
        [McpToolTrigger(nameof(ResourceLinkTool), "Returns a resource link content block.")] ToolInvocationContext context)
    {
        logger.LogInformation("ResourceLinkTool invoked");
        return new ResourceLinkBlock
        {
            Uri = "file://logo.png",
            Name = "Azure Functions Logo",
            MimeType = "image/png"
        };
    }

    /// <summary>
    /// Returns multiple content blocks of different types. Tests IList&lt;ContentBlock&gt; return.
    /// Example: { "data": "iVBORw0KGgoAAAANSUhEUg==", "mimeType": "image/jpeg" }
    /// </summary>
    [Function(nameof(MultiContentTool))]
    public IList<ContentBlock> MultiContentTool(
        [McpToolTrigger(nameof(MultiContentTool), "Returns multiple content blocks of mixed types.")] ToolInvocationContext context,
        [McpToolProperty("data", "Base64-encoded image data.", true)] string data,
        [McpToolProperty("mimeType", "Image MIME type.", false)] string? mimeType)
    {
        logger.LogInformation("MultiContentTool invoked");
        return new List<ContentBlock>
        {
            new TextContentBlock { Text = "Here is an image for you!" },
            new ResourceLinkBlock { Name = "example", Uri = "https://www.example.com/", Description = "Image Information" },
            new ImageContentBlock { Data = Encoding.UTF8.GetBytes(data), MimeType = mimeType ?? "image/jpeg" }
        };
    }

    /// <summary>
    /// Returns a CallToolResult with both content blocks and structured content.
    /// Tests explicit CallToolResult return with StructuredContent.
    /// </summary>
    [Function(nameof(StructuredContentTool))]
    public CallToolResult StructuredContentTool(
        [McpToolTrigger(nameof(StructuredContentTool), "Returns structured content with metadata and an image.")] ToolInvocationContext context)
    {
        logger.LogInformation("StructuredContentTool invoked");

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
                new TextContentBlock { Text = metadataJson },
                ImageContentBlock.FromBytes(imageBytes, "image/png")
            },
            StructuredContent = JsonSerializer.Deserialize<JsonElement>(metadataJson)
        };
    }
}
