// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated.Tools;

/// <summary>
/// Tools that test MCP tool annotations (title and behavior hints) advertised on tools/list.
/// </summary>
public class AnnotatedToolFunctions(ILogger<AnnotatedToolFunctions> logger)
{
    /// <summary>
    /// A read-only, non-destructive tool with a display title. Tests annotation emission.
    /// </summary>
    [Function(nameof(AnnotatedReadOnlyTool))]
    public string AnnotatedReadOnlyTool(
        [McpToolTrigger(nameof(AnnotatedReadOnlyTool), "Looks up a value without modifying anything.",
            Title = "Annotated Read-Only Tool", ReadOnly = true, Destructive = false)] ToolInvocationContext context,
        [McpToolProperty("key", "The key to look up.")] string key)
    {
        logger.LogInformation("AnnotatedReadOnlyTool invoked with key: {Key}", key);
        return $"value-for-{key}";
    }

    /// <summary>
    /// A destructive tool exercising explicit false and true behavior hints.
    /// </summary>
    [Function(nameof(AnnotatedDestructiveTool))]
    public string AnnotatedDestructiveTool(
        [McpToolTrigger(nameof(AnnotatedDestructiveTool), "Deletes a record and reaches out to external systems.",
            Title = "Annotated Destructive Tool", ReadOnly = false, Destructive = true)] ToolInvocationContext context,
        [McpToolProperty("id", "The record identifier to delete.")] string id)
    {
        logger.LogInformation("AnnotatedDestructiveTool invoked with id: {Id}", id);
        return $"deleted-{id}";
    }
}
