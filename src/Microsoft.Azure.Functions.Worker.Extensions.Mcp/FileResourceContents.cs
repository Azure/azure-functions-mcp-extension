// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Represents a file-backed MCP resource that should be materialized to text or blob content at runtime.
/// </summary>
public sealed class FileResourceContents
{
    /// <summary>
    /// Gets or sets the canonical URI of the resource.
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the resource.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Gets or sets the file system path to load for this resource.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// Gets metadata that should be preserved on the materialized resource contents.
    /// </summary>
    public JsonObject Meta { get; set; } = [];
}