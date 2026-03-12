// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal static class ResourceReturnValueHelper
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

    public static ReadResourceResult CreateReadResourceResult(object value, McpResourceTriggerAttribute resourceAttribute, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(resourceAttribute);
        ArgumentNullException.ThrowIfNull(logger);

        return value switch
        {
            string stringValue => CreateResultFromString(stringValue, resourceAttribute, logger),
            byte[] binaryData => CreateBlobResult(binaryData, resourceAttribute),
            FileResourceContents fileResourceContents => CreateFileResourceContentsResult(fileResourceContents, resourceAttribute),
            _ => throw new InvalidOperationException(
                $"Unsupported return type for resource read: {value.GetType().Name}. Expected string, byte[], or FileResourceContents.")
        };
    }

    private static ReadResourceResult CreateResultFromString(string value, McpResourceTriggerAttribute resourceAttribute, ILogger logger)
    {
        if (TryDeserializeWorkerResult(value, resourceAttribute, logger, out var workerResult) && workerResult is not null)
        {
            return workerResult;
        }

        return new ReadResourceResult
        {
            Contents =
            [
                new TextResourceContents
                {
                    Uri = resourceAttribute.Uri,
                    MimeType = resourceAttribute.MimeType,
                    Text = value
                }
            ]
        };
    }

    private static ReadResourceResult CreateBlobResult(byte[] binaryData, McpResourceTriggerAttribute resourceAttribute)
    {
        return new ReadResourceResult
        {
            Contents =
            [
                new BlobResourceContents
                {
                    Uri = resourceAttribute.Uri,
                    MimeType = resourceAttribute.MimeType,
                    Blob = Convert.ToBase64String(binaryData)
                }
            ]
        };
    }

    private static ReadResourceResult CreateFileResourceContentsResult(FileResourceContents fileResourceContents, McpResourceTriggerAttribute resourceAttribute)
    {
        return new ReadResourceResult
        {
            Contents = [CreateFileBackedResourceContents(fileResourceContents, resourceAttribute)]
        };
    }

    private static bool TryDeserializeWorkerResult(string jsonString, McpResourceTriggerAttribute resourceAttribute, ILogger logger, out ReadResourceResult? result)
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

            var resourceContents = DeserializeToResourceContents(mcpResult.Content, resourceAttribute);
            result = new ReadResourceResult
            {
                Contents = [resourceContents]
            };

            return true;
        }
        catch (JsonException)
        {
            logger.LogDebug("Failed to deserialize worker MCP resource result. Treating return value as plain text.");
            return false;
        }
        catch (InvalidOperationException) when (!string.Equals(mcpResult?.Content?.Trim(), "null", StringComparison.Ordinal))
        {
            logger.LogDebug("Failed to deserialize worker MCP resource result. Treating return value as plain text.");
            return false;
        }
    }

    private static ResourceContents DeserializeToResourceContents(string content, McpResourceTriggerAttribute resourceAttribute)
    {
        if (TryDeserializeResourceContents(content, resourceAttribute, out var resourceContents) && resourceContents is not null)
        {
            return resourceContents;
        }

        throw new InvalidOperationException("Failed to deserialize resource content.");
    }

    private static bool TryDeserializeResourceContents(string content, McpResourceTriggerAttribute resourceAttribute, out ResourceContents? resourceContents)
    {
        try
        {
            resourceContents = JsonSerializer.Deserialize<ResourceContents>(content, McpJsonUtilities.DefaultOptions);
            if (resourceContents is not null)
            {
                ApplyResourceDefaults(resourceContents, resourceAttribute);
                return true;
            }
        }
        catch (JsonException)
        {
        }

        try
        {
            var fileResourceContents = JsonSerializer.Deserialize<FileResourceContents>(content, McpJsonUtilities.DefaultOptions);
            if (fileResourceContents is not null)
            {
                resourceContents = CreateFileBackedResourceContents(fileResourceContents, resourceAttribute);
                return true;
            }
        }
        catch (JsonException)
        {
        }

        resourceContents = null;
        return false;
    }

    private static ResourceContents CreateFileBackedResourceContents(FileResourceContents fileResourceContents, McpResourceTriggerAttribute resourceAttribute)
    {
        if (string.IsNullOrWhiteSpace(fileResourceContents.Path))
        {
            throw new InvalidOperationException("FileResourceContents.Path must be provided.");
        }

        string uri = string.IsNullOrWhiteSpace(fileResourceContents.Uri) ? resourceAttribute.Uri : fileResourceContents.Uri;
        string? mimeType = ResolveMimeType(fileResourceContents, resourceAttribute);
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

    private static void ApplyResourceDefaults(ResourceContents resourceContents, McpResourceTriggerAttribute resourceAttribute)
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

    private static string? ResolveMimeType(FileResourceContents fileResourceContents, McpResourceTriggerAttribute resourceAttribute)
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

    private static JsonObject? CloneMetadata(JsonObject? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        return metadata.DeepClone().AsObject();
    }
}