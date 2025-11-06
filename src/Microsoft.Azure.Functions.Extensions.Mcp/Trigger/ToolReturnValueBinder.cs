// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Host.Bindings;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class ToolReturnValueBinder(CallToolExecutionContext executionContext) : IValueBinder
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

        var result = JsonSerializer.Deserialize<McpToolResult>(jsonString, McpJsonSerializerOptions.DefaultOptions)
                     ?? throw new InvalidOperationException("The function return value could not be deserialized to a valid McpToolResult.");


        if (string.IsNullOrEmpty(result.Type))
        {
            throw new InvalidOperationException($"The McpToolResult '{nameof(result.Type)}' cannot be null or empty.");
        }

        try
        {
            Collection<ContentBlock> contentBlocks = DeserializeToContentBlockCollection(result);

            if (contentBlocks.Count == 0)
            {
                throw new InvalidOperationException("No content items were produced from the McpToolResult.");
            }

            executionContext.SetResult(new CallToolResult
            {
                Content = contentBlocks
            });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize content for type '{result.Type}'.", ex);
        }

        return Task.CompletedTask;
    }

    public Task<object> GetValueAsync() => throw new NotSupportedException();

    public string ToInvokeString() => string.Empty;

    private static Collection<ContentBlock> DeserializeToContentBlockCollection(McpToolResult result)
    {
        var blocks = new Collection<ContentBlock>();

        // Explicit multi-content contract
        if (string.Equals(result.Type, McpConstants.ToolResultContentTypes.MultiContentResult, StringComparison.OrdinalIgnoreCase))
        {

            var collection = JsonSerializer.Deserialize<Collection<ContentBlock>>(result.Content!, McpJsonSerializerOptions.DefaultOptions)
                ?? throw new InvalidOperationException("Failed to deserialize multi-content result.");

            foreach (var item in collection)
            {
                blocks.Add(item);
            }
        }
        else
        {
            // Otherwise, handle single content block
            var contentBlock = result.Type switch
            {
                McpConstants.ToolResultContentTypes.Text => DeserializeContentBlock<TextContentBlock>(result.Content!),
                McpConstants.ToolResultContentTypes.Audio => DeserializeContentBlock<AudioContentBlock>(result.Content!),
                McpConstants.ToolResultContentTypes.Image => DeserializeContentBlock<ImageContentBlock>(result.Content!),
                McpConstants.ToolResultContentTypes.ResourceLink => DeserializeContentBlock<ResourceLinkBlock>(result.Content!),
                McpConstants.ToolResultContentTypes.Resource => DeserializeContentBlock<EmbeddedResourceBlock>(result.Content!),
                _ => throw new InvalidOperationException($"Unsupported content type '{result.Type}'."),
            };

            blocks.Add(contentBlock);
        }

        return blocks;
    }

    private static ContentBlock DeserializeContentBlock<T>(string json) where T : ContentBlock
    {
        var obj = JsonSerializer.Deserialize<T>(json, McpJsonUtilities.DefaultOptions);
        return obj ?? throw new InvalidOperationException($"Failed to deserialize '{typeof(T).Name}'.");
    }
}
