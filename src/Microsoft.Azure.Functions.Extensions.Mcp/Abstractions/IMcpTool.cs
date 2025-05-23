// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol.Types;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IMcpTool
{
    string Name { get; }

    public string? Description { get; set; }

    public ICollection<IMcpToolProperty> Properties { get; set; }

    Task<CallToolResponse> RunAsync(CallToolRequestParams callToolRequest, CancellationToken cancellationToken);
}