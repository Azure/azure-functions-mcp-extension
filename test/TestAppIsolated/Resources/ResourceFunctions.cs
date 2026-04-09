// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated.Resources;

/// <summary>
/// Static resource functions that test text/binary resources, metadata, and edge cases.
/// </summary>
public class ResourceFunctions(ILogger<ResourceFunctions> logger)
{
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

    /// <summary>
    /// Returns a text file. Tests string return type, full attribute set (Title, Description, MimeType),
    /// and [McpMetadata] attribute on resources.
    /// </summary>
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
        logger.LogInformation("Reading text resource from local file storage");
        var file = Path.Combine(AppContext.BaseDirectory, "assets", "readme.md");
        return File.ReadAllText(file);
    }

    /// <summary>
    /// Returns a binary image. Tests byte[] return type and image/png MIME type.
    /// </summary>
    [Function(nameof(GetImageResource))]
    public byte[] GetImageResource(
        [McpResourceTrigger(
            "file://logo.png",
            "logo",
            Description = "Azure Functions logo",
            MimeType = "image/png")] ResourceInvocationContext context)
    {
        logger.LogInformation("Reading image from local file system");
        var filePath = Path.Combine(AppContext.BaseDirectory, "assets", "logo.png");
        return File.ReadAllBytes(filePath);
    }

    /// <summary>
    /// A bare-minimum resource with only the required URI and name — no Title, Description, or MimeType.
    /// Tests that optional properties can be omitted.
    /// </summary>
    [Function(nameof(GetMinimalResource))]
    public string GetMinimalResource(
        [McpResourceTrigger("file://minimal.txt", "minimal")] ResourceInvocationContext context)
    {
        logger.LogInformation("Reading minimal resource");
        return "Minimal resource content";
    }

    /// <summary>
    /// A resource with additional metadata configured via fluent API.
    /// Tests combining attribute definition with builder-based metadata.
    /// </summary>
    [Function(nameof(GetResourceWithFluentMetadata))]
    public string GetResourceWithFluentMetadata(
        [McpResourceTrigger(
            "file://notes.txt",
            "notes",
            Description = "A resource with fluent API metadata",
            MimeType = "text/plain")] ResourceInvocationContext context)
    {
        logger.LogInformation("Reading resource with fluent metadata");
        return "This resource has additional metadata added via the fluent builder API.";
    }
}
