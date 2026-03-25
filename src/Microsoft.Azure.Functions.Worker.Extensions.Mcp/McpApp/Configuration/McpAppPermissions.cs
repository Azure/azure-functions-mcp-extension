// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Specifies the permissions granted to an MCP App view.
/// </summary>
[Flags]
public enum McpAppPermissions
{
    /// <summary>No additional permissions.</summary>
    None = 0,

    /// <summary>Allow the view to read from the clipboard.</summary>
    ClipboardRead = 1,

    /// <summary>Allow the view to write to the clipboard.</summary>
    ClipboardWrite = 2,
}
