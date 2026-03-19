// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Specifies the visibility targets for an MCP App tool.
/// </summary>
[Flags]
public enum McpVisibility
{
    /// <summary>No visibility targets.</summary>
    None = 0,

    /// <summary>Visible to the model (LLM) during tool selection.</summary>
    Model = 1,

    /// <summary>Visible to the app (host UI) for rendering.</summary>
    App = 2,
}
