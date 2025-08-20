// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Provides MCP instance identification
/// </summary>
internal interface IMcpInstanceIdProvider
{
    /// <summary>
    /// Gets the current instance ID
    /// </summary>
    string InstanceId { get; }
}
