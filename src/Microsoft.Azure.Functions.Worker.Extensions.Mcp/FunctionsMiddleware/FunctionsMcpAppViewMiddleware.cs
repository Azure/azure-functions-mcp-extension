// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Middleware that resolves MCP App views for tool invocations.
/// </summary>
internal class FunctionsMcpAppViewMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IOptionsMonitor<ToolOptions> _optionsMonitor;

    public FunctionsMcpAppViewMiddleware(IOptionsMonitor<ToolOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.TryGetMcpToolTriggerName(out var triggerName)
            || string.IsNullOrEmpty(triggerName)
            || !context.BindingContext.BindingData.TryGetValue(triggerName, out _))
        {
            await next(context);
            return;
        }

        // Try to get the tool name from the binding data
        string? toolName = null;
        if (context.TryGetToolInvocationContext(out var toolContext))
        {
            toolName = toolContext?.Name;
        }

        if (string.IsNullOrEmpty(toolName))
        {
            await next(context);
            return;
        }

        var toolOptions = _optionsMonitor.Get(toolName);

        if (toolOptions.AppOptions is null)
        {
            await next(context);
            return;
        }

        await next(context);
    }

    /// <summary>
    /// Resolves the HTML content for a view source.
    /// </summary>
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
}
