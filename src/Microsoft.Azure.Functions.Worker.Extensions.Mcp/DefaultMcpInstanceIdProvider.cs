// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Default implementation of MCP instance ID provider
/// </summary>
internal sealed class DefaultMcpInstanceIdProvider : IMcpInstanceIdProvider
{
    /// <summary>
    /// Gets the current instance ID
    /// </summary>
    public string InstanceId { get; } = Environment.MachineName + "_" + Environment.ProcessId;
}
