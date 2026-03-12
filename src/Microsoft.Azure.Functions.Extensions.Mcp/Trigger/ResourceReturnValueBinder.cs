// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class ResourceReturnValueBinder(
    ReadResourceExecutionContext executionContext,
    McpResourceTriggerAttribute resourceAttribute,
    ILogger<ResourceReturnValueBinder> logger) : IValueBinder
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();
    private static readonly HashSet<string> AdditionalTextMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
        "application/xml",
        "application/javascript",
        "application/typescript",
        "application/x-javascript",
        "application/yaml",
        "application/x-yaml",
        "application/x-sh",
        "image/svg+xml"
    };

    public Type Type { get; } = typeof(object);
    private readonly ILogger<ResourceReturnValueBinder> _logger = logger;

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
                    Text = stringValue
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
                    Blob = Convert.ToBase64String(binaryData)
                }]
            };

            executionContext.SetResult(blobResult);
            return Task.CompletedTask;
        }

        if (value is FileResourceContents fileResourceContents)
        {
            executionContext.SetResult(new ReadResourceResult
            {
                Contents = [CreateFileResourceContents(fileResourceContents)]
            });
            return Task.CompletedTask;
        }

        throw new InvalidOperationException($"Unsupported return type for resource read: {value.GetType().Name}. Expected string, byte[], or FileResourceContents.");
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
        McpResourceResult? mcpResult = null;

        try
        {
            mcpResult = JsonSerializer.Deserialize<McpResourceResult>(jsonString, McpJsonSerializerOptions.DefaultOptions);
                    
            if (mcpResult is null)
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
            _logger.LogDebug("Failed to deserialize worker MCP resource result. Treating return value as plain text.");
            return false;
        }
        catch (InvalidOperationException) when (!string.Equals(mcpResult?.Content?.Trim(), "null", StringComparison.Ordinal))
        {
            _logger.LogDebug("Failed to deserialize worker MCP resource result. Treating return value as plain text.");
            return false;
        }
    }

    private ResourceContents DeserializeToResourceContents(McpResourceResult result)
    {
        if (TryDeserializeResourceContents(result.Content, out var resourceContents) && resourceContents is not null)
        {
            return resourceContents;
        }

        throw new InvalidOperationException("Failed to deserialize resource content.");
    }

    private bool TryDeserializeResourceContents(string content, out ResourceContents? resourceContents)
    {
        try
        {
            resourceContents = JsonSerializer.Deserialize<ResourceContents>(content, McpJsonUtilities.DefaultOptions);
            if (resourceContents is not null)
            {
                ApplyResourceDefaults(resourceContents);
                return true;
            }
        }
        catch (JsonException)
        {
        }

        try
        {
            FileResourceContents? fileResourceContents = JsonSerializer.Deserialize<FileResourceContents>(content, McpJsonUtilities.DefaultOptions);
            if (fileResourceContents is not null)
            {
                resourceContents = CreateFileResourceContents(fileResourceContents);
                return true;
            }
        }
        catch (JsonException)
        {
        }

        resourceContents = null;
        return false;
    }

    private ResourceContents CreateFileResourceContents(FileResourceContents fileResourceContents)
    {
        if (string.IsNullOrWhiteSpace(fileResourceContents.Path))
        {
            throw new InvalidOperationException("FileResourceContents.Path must be provided.");
        }

        string uri = string.IsNullOrWhiteSpace(fileResourceContents.Uri) ? resourceAttribute.Uri : fileResourceContents.Uri;
        string? mimeType = ResolveMimeType(fileResourceContents);
        JsonObject? metadata = CloneMetadata(fileResourceContents.Meta);

        if (IsTextMimeType(mimeType))
        {
            var textResourceContents = new TextResourceContents
            {
                Uri = uri,
                MimeType = mimeType,
                Text = File.ReadAllText(fileResourceContents.Path)
            };

            if (metadata is not null)
            {
                textResourceContents.Meta = metadata;
            }

            return textResourceContents;
        }

        var blobResourceContents = new BlobResourceContents
        {
            Uri = uri,
            MimeType = mimeType,
            Blob = Convert.ToBase64String(File.ReadAllBytes(fileResourceContents.Path))
        };

        if (metadata is not null)
        {
            blobResourceContents.Meta = metadata;
        }

        return blobResourceContents;
    }

    private void ApplyResourceDefaults(ResourceContents resourceContents)
    {
        if (string.IsNullOrEmpty(resourceContents.Uri))
        {
            resourceContents.Uri = resourceAttribute.Uri;
        }

        if (string.IsNullOrEmpty(resourceContents.MimeType))
        {
            resourceContents.MimeType = resourceAttribute.MimeType;
        }
    }

    private string? ResolveMimeType(FileResourceContents fileResourceContents)
    {
        if (!string.IsNullOrWhiteSpace(fileResourceContents.MimeType))
        {
            return fileResourceContents.MimeType;
        }

        if (!string.IsNullOrWhiteSpace(resourceAttribute.MimeType))
        {
            return resourceAttribute.MimeType;
        }

        if (ContentTypeProvider.TryGetContentType(fileResourceContents.Path, out var mimeType))
        {
            return mimeType;
        }

        return "application/octet-stream";
    }

    private static bool IsTextMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            return false;
        }

        return mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || mimeType.EndsWith("+json", StringComparison.OrdinalIgnoreCase)
            || mimeType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase)
            || mimeType.EndsWith("+yaml", StringComparison.OrdinalIgnoreCase)
            || AdditionalTextMimeTypes.Contains(mimeType);
    }

    private static JsonObject? CloneMetadata(JsonObject metadata)
    {
        if (metadata.Count == 0)
        {
            return null;
        }

        return metadata.DeepClone().AsObject();
    }
}
