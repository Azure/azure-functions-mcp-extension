// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Content Security Policy configuration for an MCP App view.
/// </summary>
public class CspOptions
{
    /// <summary>Origins allowed for connect-src.</summary>
    public List<string> ConnectSources { get; } = new();

    /// <summary>Origins allowed for default-src (general resource loading).</summary>
    public List<string> ResourceSources { get; } = new();

    /// <summary>Origins allowed for script-src.</summary>
    public List<string> ScriptSources { get; } = new();

    /// <summary>Origins allowed for style-src.</summary>
    public List<string> StyleSources { get; } = new();
}
