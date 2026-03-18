// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Configures the Content Security Policy for an MCP App view.
/// </summary>
public interface IMcpCspBuilder
{
    /// <summary>
    /// Adds an external origin the view is allowed to connect to.
    /// </summary>
    IMcpCspBuilder ConnectTo(string domain);

    /// <summary>
    /// Adds an external origin the view is allowed to load resources from.
    /// </summary>
    IMcpCspBuilder LoadResourcesFrom(string domain);
}
