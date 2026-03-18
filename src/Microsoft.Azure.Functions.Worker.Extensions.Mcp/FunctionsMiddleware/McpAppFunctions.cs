// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Static entry-point methods for synthetic MCP App HTTP functions.
/// These methods are referenced by <see cref="McpAppFunctionMetadataFactory"/> and
/// invoked by the Azure Functions host when requests hit the synthetic routes.
/// </summary>
internal static class McpAppFunctions
{
#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file
    internal static readonly string ScriptFile = Path.GetFileName(typeof(McpAppFunctions).Assembly.Location);
#pragma warning restore IL3000

    internal static readonly string ServeViewEntryPoint = $"{typeof(McpAppFunctions).FullName!}.{nameof(ServeViewAsync)}";
    internal static readonly string ServeStaticAssetEntryPoint = $"{typeof(McpAppFunctions).FullName!}.{nameof(ServeStaticAssetAsync)}";

    /// <summary>
    /// Serves the HTML content for an MCP App view.
    /// </summary>
    public static async Task<HttpResponseData> ServeViewAsync(
        HttpRequestData req,
        FunctionContext context)
    {
        var toolName = McpAppUtilities.ExtractToolName(context.FunctionDefinition.Name);

        var optionsMonitor = context.InstanceServices.GetRequiredService<IOptionsMonitor<ToolOptions>>();
        var toolOptions = optionsMonitor.Get(toolName);

        if (toolOptions.AppOptions is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        // Resolve view name from query string, defaulting to empty string (default view)
        string viewName = string.Empty;
        var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var requestedView = queryParams["view"];
        if (!string.IsNullOrEmpty(requestedView))
        {
            viewName = requestedView;
        }

        if (!toolOptions.AppOptions.Views.TryGetValue(viewName, out var view))
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            await response.WriteStringAsync(
                $"View '{viewName}' not found for MCP App tool '{toolName}'. " +
                $"Available views: {string.Join(", ", toolOptions.AppOptions.Views.Keys.Select(k => string.IsNullOrEmpty(k) ? "(default)" : k))}");
            return response;
        }

        if (view.Source is null)
        {
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }

        try
        {
            var html = await ResolveViewContentAsync(view.Source, toolName, viewName, context.CancellationToken);

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            okResponse.Headers.Add("Content-Type", "text/html; charset=utf-8");
            await okResponse.WriteStringAsync(html);
            return okResponse;
        }
        catch (InvalidOperationException ex)
        {
            var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errResponse.WriteStringAsync(ex.Message);
            return errResponse;
        }
    }

    /// <summary>
    /// Serves a static asset file for an MCP App.
    /// </summary>
    public static async Task<HttpResponseData> ServeStaticAssetAsync(
        HttpRequestData req,
        FunctionContext context)
    {
        var functionName = context.FunctionDefinition.Name;
        var toolName = functionName[McpAppUtilities.AssetsPrefixLength..];

        var optionsMonitor = context.InstanceServices.GetRequiredService<IOptionsMonitor<ToolOptions>>();
        var toolOptions = optionsMonitor.Get(toolName);

        if (toolOptions.AppOptions?.StaticAssetsDirectory is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        // Extract the asset path from the route
        if (!context.BindingContext.BindingData.TryGetValue("path", out var pathObj)
            || pathObj is not string requestedPath
            || string.IsNullOrEmpty(requestedPath))
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var resolvedPath = SafePathResolver.Resolve(
            requestedPath,
            toolOptions.AppOptions.StaticAssetsDirectory,
            toolOptions.AppOptions.StaticAssets);

        if (resolvedPath is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var contentType = GetContentType(resolvedPath);
        var bytes = await File.ReadAllBytesAsync(resolvedPath, context.CancellationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", contentType);
        await response.Body.WriteAsync(bytes, context.CancellationToken);
        return response;
    }

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

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".html" or ".htm" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" or ".mjs" => "application/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".eot" => "application/vnd.ms-fontobject",
            ".map" => "application/json",
            ".wasm" => "application/wasm",
            ".xml" => "application/xml",
            ".txt" => "text/plain; charset=utf-8",
            _ => "application/octet-stream",
        };
    }
}
