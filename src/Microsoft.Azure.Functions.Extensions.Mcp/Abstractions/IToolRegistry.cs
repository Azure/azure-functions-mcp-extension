// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;

internal interface IToolRegistry
{
    void Register(IMcpTool toolListener);

    bool TryGetTool(string name, [NotNullWhen(true)] out IMcpTool? tool);

    ICollection<IMcpTool> GetTools();

    ValueTask<ListToolsResult> ListToolsAsync(CancellationToken cancellationToken = default);
}
