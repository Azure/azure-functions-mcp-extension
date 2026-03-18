// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Browser permissions requested for an MCP App view.
/// Hosts MAY honor these by setting appropriate iframe allow attributes.
/// Maps to _meta.ui.permissions.
/// </summary>
[Flags]
public enum McpAppPermissions
{
    /// <summary>No additional permissions requested.</summary>
    None = 0,

    /// <summary>Requests clipboard-write permission.</summary>
    ClipboardWrite = 1,

    /// <summary>Requests clipboard-read permission.</summary>
    ClipboardRead = 2
}
