// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
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

        if (value is FileResourceContents fileContents)
        {
            var convertedContents = ConvertFileResourceContents(fileContents);
            var fileResult = new ReadResourceResult
            {
                Contents = [convertedContents]
            };

            executionContext.SetResult(fileResult);
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

        try
        {
            var mcpResult = JsonSerializer.Deserialize<McpResourceResult>(jsonString, McpJsonSerializerOptions.DefaultOptions);
                    
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
    }

    private ResourceContents DeserializeToResourceContents(McpResourceResult result)
    {
        ResourceContents resourceContents = JsonSerializer.Deserialize<ResourceContents>(result.Content, McpJsonUtilities.DefaultOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize resource content.");

        // Ensure URI and MimeType are set from attribute if not in the content returned or if empty
        if (string.IsNullOrEmpty(resourceContents.Uri))
        {
            resourceContents.Uri = resourceAttribute.Uri;
        }

        if (string.IsNullOrEmpty(resourceContents.MimeType))
        {
            resourceContents.MimeType = resourceAttribute.MimeType;
        }

        return resourceContents;
    }

    private ResourceContents ConvertFileResourceContents(FileResourceContents fileContents)
    {
        if (string.IsNullOrEmpty(fileContents.Path))
        {
            throw new InvalidOperationException("FileResourceContents.Path cannot be null or empty.");
        }

        if (!File.Exists(fileContents.Path))
        {
            throw new FileNotFoundException($"File not found: {fileContents.Path}", fileContents.Path);
        }

        // Use attribute values as fallback for Uri and MimeType
        var uri = string.IsNullOrEmpty(fileContents.Uri) ? resourceAttribute.Uri : fileContents.Uri;
        var mimeType = string.IsNullOrEmpty(fileContents.MimeType) ? resourceAttribute.MimeType : fileContents.MimeType;

        bool isTextType = IsTextMimeType(mimeType ?? string.Empty);

        if (isTextType)
        {
            var text = File.ReadAllText(fileContents.Path);
            return new TextResourceContents
            {
                Uri = uri,
                MimeType = mimeType,
                Text = text,
                Meta = fileContents.Meta
            };
        }
        else
        {
            var bytes = File.ReadAllBytes(fileContents.Path);
            return new BlobResourceContents
            {
                Uri = uri,
                MimeType = mimeType,
                Blob = Convert.ToBase64String(bytes),
                Meta = fileContents.Meta
            };
        }
    }

    private static bool IsTextMimeType(string mimeType)
    {
        if (string.IsNullOrEmpty(mimeType))
        {
            return false;
        }

        var lowerMimeType = mimeType.ToLowerInvariant();
        
        // Check for text/* types
        if (lowerMimeType.StartsWith("text/"))
        {
            return true;
        }

        // Check for common text-based types that don't start with text/
        return lowerMimeType switch
        {
            "application/json" => true,
            "application/xml" => true,
            "application/javascript" => true,
            "application/ecmascript" => true,
            "application/x-javascript" => true,
            "application/x-sh" => true,
            _ when lowerMimeType.EndsWith("+xml") => true,
            _ when lowerMimeType.EndsWith("+json") => true,
            _ => false
        };
    }
}
