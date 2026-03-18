// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Configures the Content Security Policy for an MCP App view.
/// </summary>
public interface IMcpCspBuilder
{
    /// <summary>Adds an origin to connect-src.</summary>
    IMcpCspBuilder ConnectTo(string origin);

    /// <summary>Adds an origin to default-src (general resource loading).</summary>
    IMcpCspBuilder LoadResourcesFrom(string origin);

    /// <summary>Adds an origin to script-src.</summary>
    IMcpCspBuilder LoadScriptsFrom(string origin);

    /// <summary>Adds an origin to style-src.</summary>
    IMcpCspBuilder LoadStylesFrom(string origin);
}
