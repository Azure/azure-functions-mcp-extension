// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Represents a file-based resource that will be automatically converted to either 
/// TextResourceContents or BlobResourceContents based on the MIME type.
/// </summary>
public sealed class FileResourceContents
{
    /// <summary>
    /// Gets or sets the URI of the resource.
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the resource.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Gets or sets the metadata for the resource.
    /// </summary>
    public JsonObject? Meta { get; set; }

    /// <summary>
    /// Gets or sets the file system path to the file to be loaded.
    /// </summary>
    public required string Path { get; set; }
}
