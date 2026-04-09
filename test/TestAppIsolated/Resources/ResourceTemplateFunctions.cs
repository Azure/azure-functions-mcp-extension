// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated.Resources;

/// <summary>
/// Resource template functions that test URI template parameter extraction.
/// </summary>
public class ResourceTemplateFunctions(ILogger<ResourceTemplateFunctions> logger)
{
    /// <summary>
    /// A resource template with a single URI parameter.
    /// Tests parameter extraction from URI templates.
    /// </summary>
    [Function(nameof(UserProfileResourceTemplate))]
    public string UserProfileResourceTemplate(
        [McpResourceTrigger(
            "user://profile/{name}",
            "userProfile",
            Description = "User profile resource",
            MimeType = "application/json")] ResourceInvocationContext context, string name)
    {
        logger.LogInformation("Reading user profile template for {Name}", name);
        var file = Path.Combine(AppContext.BaseDirectory, "assets", $"{name}.md");
        return File.ReadAllText(file);
    }

    /// <summary>
    /// A resource template with multiple parameters separated by literal text (not URI delimiters).
    /// Tests complex URI template parsing: store://catalog/{category}items{tag}.
    /// </summary>
    [Function(nameof(CatalogItemResource))]
    public string CatalogItemResource(
        [McpResourceTrigger(
            "store://catalog/{category}items{tag}",
            "catalogItem",
            Description = "Catalog item lookup by category and tag",
            MimeType = "application/json")] ResourceInvocationContext context, string category, string tag)
    {
        logger.LogInformation("Looking up catalog item: category={Category}, tag={Tag}", category, tag);
        return $"{{\"category\":\"{category}\",\"tag\":\"{tag}\"}}";
    }
}
