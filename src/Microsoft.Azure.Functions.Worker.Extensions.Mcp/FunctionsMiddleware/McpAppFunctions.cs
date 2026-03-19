// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Static entry-point methods for synthetic MCP App resource functions.
/// These methods are referenced by <see cref="McpAppFunctionMetadataFactory"/> and
/// invoked by the Azure Functions host when the MCP host issues <c>resources/read</c>
/// for a <c>ui://</c> resource.
/// </summary>
internal static class McpAppFunctions
{
#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file
    internal static readonly string ScriptFile = Path.GetFileName(typeof(McpAppFunctions).Assembly.Location);
#pragma warning restore IL3000

    internal static readonly string ServeViewEntryPoint = $"{typeof(McpAppFunctions).FullName!}.{nameof(ServeView)}";

    public static void ServeView() { }

    internal static async Task<string> ResolveViewContentAsync(
        McpViewSource source,
        string toolName,
        string viewName,
        CancellationToken ct)
    {
        return source switch
        {
            FileViewSource { Path: var path } =>
                await File.ReadAllTextAsync(path, ct),

            EmbeddedViewSource { Assembly: var asm, ResourceName: var resName } =>
                ReadEmbeddedResource(asm, resName),

            InlineHtmlViewSource { Html: var html } => html,

            null =>
                throw new InvalidOperationException(
                    $"View source is null for tool '{toolName}', view '{viewName}'. " +
                    $"This should have been caught by ToolOptionsValidator at startup."),

            _ =>
                throw new InvalidOperationException(
                    $"Unknown McpViewSource type: {source.GetType().Name}. " +
                    $"Only FileViewSource and EmbeddedViewSource are supported.")
        };
    }

    internal static string ReadEmbeddedResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found in assembly '{assembly.FullName}'. " +
                $"Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

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
