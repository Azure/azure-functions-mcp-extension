// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Placeholder entry point and utility methods for synthetic MCP App resource functions.
/// The actual view serving is handled by <see cref="FunctionsMcpAppMiddleware"/>.
/// </summary>
internal static class McpAppFunctions
{
#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file
    internal static readonly string ScriptFile = Path.GetFileName(typeof(McpAppFunctions).Assembly.Location);
#pragma warning restore IL3000

    internal static readonly string ServeViewEntryPoint = $"{typeof(McpAppFunctions).FullName!}.{nameof(ServeView)}";

    /// <summary>
    /// Placeholder entry point for synthetic MCP App resource functions.
    /// Never reached at runtime — <see cref="FunctionsMcpAppMiddleware"/> short-circuits first.
    /// </summary>
    public static void ServeView()
        => throw new NotSupportedException("Calling this method directly is not supported.");

    /// <summary>
    /// Builds the <c>_meta.ui</c> object for resource response metadata,
    /// containing CSP, permissions, border preference, and domain per the MCP Apps spec.
    /// Returns null if the view has no metadata to emit.
    /// </summary>
    internal static McpAppUiMeta? BuildResourceUiMeta(ViewOptions viewOptions)
    {
        McpAppCsp? csp = null;
        if (viewOptions.Csp is not null)
        {
            csp = new McpAppCsp
            {
                ConnectDomains = viewOptions.Csp.ConnectDomains.Count > 0 ? viewOptions.Csp.ConnectDomains : null,
                ResourceDomains = viewOptions.Csp.ResourceDomains.Count > 0 ? viewOptions.Csp.ResourceDomains : null,
                FrameDomains = viewOptions.Csp.FrameDomains.Count > 0 ? viewOptions.Csp.FrameDomains : null,
                BaseUriDomains = viewOptions.Csp.BaseUriDomains.Count > 0 ? viewOptions.Csp.BaseUriDomains : null,
            };
        }

        McpAppPermissionsMap? permissions = null;
        if (viewOptions.Permissions != McpAppPermissions.None)
        {
            permissions = new McpAppPermissionsMap
            {
                ClipboardRead = viewOptions.Permissions.HasFlag(McpAppPermissions.ClipboardRead) ? new EmptyObject() : null,
                ClipboardWrite = viewOptions.Permissions.HasFlag(McpAppPermissions.ClipboardWrite) ? new EmptyObject() : null,
            };
        }

        if (csp is null && permissions is null && viewOptions.PrefersBorder is null && viewOptions.Domain is null)
        {
            return null;
        }

        return new McpAppUiMeta
        {
            Csp = csp,
            Permissions = permissions,
            PrefersBorder = viewOptions.PrefersBorder,
            Domain = viewOptions.Domain,
        };
    }
}
