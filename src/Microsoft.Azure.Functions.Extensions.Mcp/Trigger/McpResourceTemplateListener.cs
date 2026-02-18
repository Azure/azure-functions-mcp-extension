// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using Microsoft.Azure.WebJobs.Host.Executors;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Listener for MCP resource templates with RFC 6570-style URI patterns.
/// </summary>
internal sealed class McpResourceTemplateListener(
    ITriggeredFunctionExecutor executor,
    string functionName,
    string resourceUri,
    string resourceName,
    string? resourceDescription,
    string? resourceMimeType,
    long? resourceSize,
    IReadOnlyDictionary<string, object?> metadata,
    Regex templateRegex)
    : McpResourceListener(executor, functionName, resourceUri, resourceName, resourceDescription, resourceMimeType, resourceSize, metadata),
      IMcpResourceTemplate
{
    /// <inheritdoc/>
    public Regex TemplateRegex { get; } = templateRegex;
}
