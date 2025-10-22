// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Host.Bindings;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal class McpToolTriggerRichContentReturnValueBinder(CallToolExecutionContext executionContext) : IValueBinder
{
    public Type Type { get; } = typeof(object);

    public Task SetValueAsync(object value, CancellationToken cancellationToken)
    {
        if (value is null)
        {
            executionContext.SetResult(null!);
            return Task.CompletedTask;
        }

        if (value is not string jsonString)
        {
            throw new ArgumentException("Expected JSON string.", nameof(value));
        }

        var mcpToolResult = JsonSerializer.Deserialize<McpToolResult>(jsonString, McpJsonSerializerOptions.DefaultOptions)
                    ?? throw new InvalidOperationException("The function return value could not be deserialized to a valid McpToolResult.");

        if (string.IsNullOrWhiteSpace(mcpToolResult.Content))
        {
            throw new InvalidOperationException("McpToolResult is missing content.");
        }

        // Do we want to handle raw content type differently?
        // Or should it just be a TextContentBlock with Text set to the raw content?
        if (mcpToolResult.Type is McpContentToolType.Raw)
        {
            // EITHER use return the raw content directly, not wrapped in CallToolResult
            //     executionContext.SetResult(mcpToolResult.Content);
            // OR use structured content?
            var rawContentResult = new CallToolResult
            {
                Content = [],
                StructuredContent = mcpToolResult.Content
            };
            executionContext.SetResult(rawContentResult);
            return Task.CompletedTask;
        }

        ContentBlock? content;
        try
        {
            content = mcpToolResult.Type switch
            {
                McpContentToolType.Text => JsonSerializer.Deserialize<TextContentBlock>(mcpToolResult.Content),
                McpContentToolType.Audio => JsonSerializer.Deserialize<AudioContentBlock>(mcpToolResult.Content),
                McpContentToolType.Image => JsonSerializer.Deserialize<ImageContentBlock>(mcpToolResult.Content),
                McpContentToolType.ResourceLink => JsonSerializer.Deserialize<ResourceLinkBlock>(mcpToolResult.Content),
                _ => throw new InvalidOperationException($"Unsupported content type '{mcpToolResult.Type}'."),
            };

            if (content is null)
            {
                throw new InvalidOperationException($"Failed to deserialize content for type '{mcpToolResult.Type}'.");
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize content for type '{mcpToolResult.Type}'.", ex);
        }

        executionContext.SetResult(new CallToolResult
        {
            Content = [content]
        });

        return Task.CompletedTask;
    }

    public Task<object> GetValueAsync()
    {
        throw new NotSupportedException();
    }

    public string ToInvokeString()
    {
        return string.Empty;
    }
}
