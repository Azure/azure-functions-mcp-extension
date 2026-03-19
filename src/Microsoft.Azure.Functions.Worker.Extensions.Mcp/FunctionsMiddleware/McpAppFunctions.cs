// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json.Nodes;
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
    /// Builds the <c>_meta.ui</c> JSON object for resource response metadata,
    /// containing CSP, permissions, border preference, and domain per the MCP Apps spec.
    /// </summary>
    internal static JsonObject BuildResourceUiMeta(ViewOptions viewOptions)
    {
        var uiMeta = new JsonObject();

        if (viewOptions.Csp is not null)
        {
            uiMeta["csp"] = BuildCspNode(viewOptions.Csp);
        }

        if (viewOptions.Permissions != McpAppPermissions.None)
        {
            uiMeta["permissions"] = BuildPermissionsNode(viewOptions.Permissions);
        }

        if (viewOptions.PrefersBorder is not null)
        {
            uiMeta["prefersBorder"] = viewOptions.PrefersBorder.Value;
        }

        if (viewOptions.Domain is not null)
        {
            uiMeta["domain"] = viewOptions.Domain;
        }

        return uiMeta;
    }

    private static JsonObject BuildCspNode(CspOptions csp)
    {
        var node = new JsonObject();
        if (csp.ConnectDomains.Count > 0)
        {
            node["connectDomains"] = new JsonArray(csp.ConnectDomains.Select(s => (JsonNode)s!).ToArray());
        }

        if (csp.ResourceDomains.Count > 0)
        {
            node["resourceDomains"] = new JsonArray(csp.ResourceDomains.Select(s => (JsonNode)s!).ToArray());
        }

        if (csp.FrameDomains.Count > 0)
        {
            node["frameDomains"] = new JsonArray(csp.FrameDomains.Select(s => (JsonNode)s!).ToArray());
        }

        if (csp.BaseUriDomains.Count > 0)
        {
            node["baseUriDomains"] = new JsonArray(csp.BaseUriDomains.Select(s => (JsonNode)s!).ToArray());
        }

        return node;
    }

    private static JsonObject BuildPermissionsNode(McpAppPermissions permissions)
    {
        var node = new JsonObject();
        if (permissions.HasFlag(McpAppPermissions.ClipboardRead))
        {
            node["clipboardRead"] = new JsonObject();
        }

        if (permissions.HasFlag(McpAppPermissions.ClipboardWrite))
        {
            node["clipboardWrite"] = new JsonObject();
        }

        return node;
    }
}
