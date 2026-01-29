// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Validation;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IMcpTool
{
    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets or sets the description of the tool.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets the input schema for the tool properties.
    /// </summary>
    public ToolInputSchema ToolInputSchema { get; }

    /// <summary>
    /// Gets the metadata dictionary associated with the tool.
    /// </summary>
    IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Gets the input schema for the tool properties.
    /// </summary>
    /// <param name="callToolRequest"> The call tool request context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the outcome of the tool execution.</returns>
    Task<CallToolResult> RunAsync(RequestContext<CallToolRequestParams> callToolRequest, CancellationToken cancellationToken);
}
