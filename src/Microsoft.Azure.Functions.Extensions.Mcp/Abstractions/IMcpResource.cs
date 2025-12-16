// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Represents a Model Context Protocol (MCP) resource.
/// </summary>
internal interface IMcpResource
{
    /// <summary>
    /// Gets the unique URI of the resource.
    /// </summary>
    string Uri { get; }

    /// <summary>
    /// Gets or sets the name of the resource.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets or sets the MIME type of the resource.
    /// </summary>
    string? MimeType { get; }

    /// <summary>
    /// Gets or sets the description of the resource.
    /// </summary>
    string? Description { get; }
    
    /// <summary>
    /// Gets or sets the size of the resource in bytes.
    /// </summary>
    long? Size { get; }

    /// <summary>
    /// Gets or sets metadata properties associated with the resource.
    /// </summary>
    IReadOnlyCollection<KeyValuePair<string, object?>> Metadata { get; }

    /// <summary>
    /// Handles a read request for the resource.
    /// </summary>
    /// <param name="readResourceRequest">The read resource request context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous read operation. The task result contains the read resource result.</returns>
    Task<ReadResourceResult> ReadAsync(RequestContext<ReadResourceRequestParams> readResourceRequest, CancellationToken cancellationToken);
}