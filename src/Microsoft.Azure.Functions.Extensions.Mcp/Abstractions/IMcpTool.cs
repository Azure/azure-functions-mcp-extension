// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Validation;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IMcpTool
{
    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a human-readable description of the tool.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets the input schema for the tool based on its defined properties.
    /// </summary>
    public ToolInputSchema ToolInputSchema { get; }

    /// <summary>
    /// Optional metadata to expose for the tool when listed via MCP.
    /// </summary>
    public JsonObject? Metadata { get; }

    /// <summary>
    /// Runs the tool with the specified invocation context.
    /// </summary>
    Task<CallToolResult> RunAsync(RequestContext<CallToolRequestParams> callToolRequest, CancellationToken cancellationToken);
}
