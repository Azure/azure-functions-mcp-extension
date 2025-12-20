// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Validation;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IMcpTool
{
    string Name { get; }

    public string? Description { get; set; }

    public ToolInputSchema ToolInputSchema { get; }

    Task<CallToolResult> RunAsync(RequestContext<CallToolRequestParams> callToolRequest, CancellationToken cancellationToken);
}
