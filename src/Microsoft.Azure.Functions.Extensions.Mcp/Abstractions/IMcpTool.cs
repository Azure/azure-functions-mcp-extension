// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IMcpTool
{
    string Name { get; }

    public string? Description { get; set; }

    public ICollection<IMcpToolProperty> Properties { get; set; }

    Task<CallToolResult> RunAsync(RequestContext<CallToolRequestParams> callToolRequest, CancellationToken cancellationToken);
}
