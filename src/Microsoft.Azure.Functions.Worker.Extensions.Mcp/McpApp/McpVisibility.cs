// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Controls where an MCP App tool is visible within the host.
/// Maps to the _meta.ui.visibility array in the MCP protocol.
/// </summary>
public enum McpVisibility
{
    /// <summary>
    /// Tool is visible to and callable by the agent only.
    /// Maps to _meta.ui.visibility: ["model"]
    /// </summary>
    Model,

    /// <summary>
    /// Tool is callable by the MCP App UI only; hidden from the agent's tool list.
    /// Maps to _meta.ui.visibility: ["app"]
    /// </summary>
    App,

    /// <summary>
    /// Tool is visible to both the agent and the MCP App UI (default).
    /// Maps to _meta.ui.visibility: ["model", "app"]
    /// </summary>
    ModelAndApp
}
