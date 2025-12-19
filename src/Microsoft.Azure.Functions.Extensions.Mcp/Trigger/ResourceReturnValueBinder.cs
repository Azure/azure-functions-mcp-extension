// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Host.Bindings;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class ResourceReturnValueBinder(ReadResourceExecutionContext executionContext, McpResourceTriggerAttribute resourceAttribute) : IValueBinder
{
    public Type Type { get; } = typeof(object);

    public Task SetValueAsync(object value, CancellationToken cancellationToken)
    {
        if (value is null)
        {
            executionContext.SetResult(null!);
            return Task.CompletedTask;
        }

        if (value is string stringValue)
        {
            // Try to deserialize as McpResourceResult from worker
            if (TryDeserializeWorkerResult(stringValue, out var workerResult) && workerResult is not null)
            {
                executionContext.SetResult(workerResult);
                return Task.CompletedTask;
            }

            // Otherwise treat as simple text content
            var textResult = new ReadResourceResult
            {
                Contents = [new TextResourceContents
                {
                    Uri = resourceAttribute.Uri,
                    MimeType = resourceAttribute.MimeType,
                    Text = stringValue,
                    Meta = null // TODO: support metadata via attribute in future PR
                }]
            };

            executionContext.SetResult(textResult);
            return Task.CompletedTask;
        }

        if (value is byte[] binaryData)
        {
            var blobResult = new ReadResourceResult
            {
                Contents = [new BlobResourceContents
                {
                    Uri = resourceAttribute.Uri,
                    MimeType = resourceAttribute.MimeType,
                    Blob = Convert.ToBase64String(binaryData),
                    Meta = null // TODO: support metadata via attribute in future PR
                }]
            };

            executionContext.SetResult(blobResult);
            return Task.CompletedTask;
        }

        throw new InvalidOperationException($"Unsupported return type for resource read: {value.GetType().Name}. Expected string or byte[].");
    }

    public Task<object> GetValueAsync()
    {
        throw new NotSupportedException();
    }

    public string ToInvokeString()
    {
        return string.Empty;
    }

    private bool TryDeserializeWorkerResult(string jsonString, out ReadResourceResult? result)
    {
        result = null;

        try
        {
            var mcpResult = JsonSerializer.Deserialize<McpResourceResult>(jsonString, McpJsonSerializerOptions.DefaultOptions);
                    
            if (mcpResult is null || string.IsNullOrEmpty(mcpResult.Type))
            {
                return false;
            }

            ResourceContents resourceContents = DeserializeToResourceContents(mcpResult);

            result = new ReadResourceResult
            {
                Contents = [resourceContents]
            };

            return true;
        }
        catch (JsonException)
        {
            // Not a valid McpResourceResult, will be treated as simple string
            return false;
        }
    }

    private ResourceContents DeserializeToResourceContents(McpResourceResult result)
    {
        ResourceContents resourceContents = JsonSerializer.Deserialize<ResourceContents>(result.Content, McpJsonUtilities.DefaultOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize resource content type '{result.Type}'.");

        // Ensure URI and MimeType are set from attribute if not in the content returned or if empty
        if (string.IsNullOrEmpty(resourceContents.Uri))
        {
            resourceContents.Uri = resourceAttribute.Uri;
        }

        if (string.IsNullOrEmpty(resourceContents.MimeType))
        {
            resourceContents.MimeType = resourceAttribute.MimeType;
        }

        // TODO: If metadata is provided via the attribute, but null in the content, set it here.
        // Alternatively, consider merging attribute metadata with content metadata.

        return resourceContents;
    }
}
