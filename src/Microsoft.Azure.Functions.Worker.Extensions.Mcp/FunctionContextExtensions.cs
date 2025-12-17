// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Provides extension methods to work with <see cref="FunctionContext"/>.
/// </summary>
internal static class FunctionContextExtensions
{
    private const string BindingAttribute = "bindingAttribute";

    /// <summary>
    /// Gets the <see cref="ToolInvocationContext"/> for the <see cref="FunctionContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="FunctionContext"/>.</param>
    /// <param name="request">The <see cref="ToolInvocationContext"/> for the context.</param>
    /// <returns></returns>
    internal static bool TryGetToolInvocationContext(this FunctionContext context, [NotNullWhen(true)] out ToolInvocationContext? toolContext)
    {
        toolContext = null;

        if (context.Items.TryGetValue(Constants.ToolInvocationContextKey, out var tic)
            && tic is ToolInvocationContext toolInvocationContext)
        {
            toolContext = toolInvocationContext;
        }

        return toolContext is not null;
    }

    /// <summary>
    /// Gets the <see cref="ResourceInvocationContext"/> for the <see cref="FunctionContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="FunctionContext"/>.</param>
    /// <param name="resourceContext">The <see cref="ResourceInvocationContext"/> for the context.</param>
    /// <returns>true if the resource context was found; otherwise, false.</returns>
    internal static bool TryGetResourceInvocationContext(this FunctionContext context, [NotNullWhen(true)] out ResourceInvocationContext? resourceContext)
    {
        resourceContext = null;

        if (context.Items.TryGetValue(Constants.ResourceInvocationContextKey, out var ric)
            && ric is ResourceInvocationContext resourceInvocationContext)
        {
            resourceContext = resourceInvocationContext;
        }

        return resourceContext is not null;
    }

    /// <summary>
    /// Gets the name of the trigger binding with the <see cref="McpToolTriggerAttribute"/> from the <see cref="FunctionContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="FunctionContext"/>.</param>
    /// <param name="triggerName">The name of the MCP tool trigger.</param>
    /// <returns>true if the trigger name was found; otherwise, false.</returns>
    internal static bool TryGetMcpToolTriggerName(this FunctionContext context, [NotNullWhen(true)] out string? triggerName)
    {
        triggerName = context.FunctionDefinition.InputBindings.Values
            .FirstOrDefault(b => b.Type.Equals(Constants.McpToolTriggerBindingType, StringComparison.OrdinalIgnoreCase))
            ?.Name;

        return triggerName is not null;
    }

    /// <summary>
    /// Gets the name of the trigger binding with the <see cref="McpResourceTriggerAttribute"/> from the <see cref="FunctionContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="FunctionContext"/>.</param>
    /// <param name="triggerName">The name of the MCP resource trigger.</param>
    /// <returns>true if the trigger name was found; otherwise, false.</returns>
    internal static bool TryGetMcpResourceTriggerName(this FunctionContext context, [NotNullWhen(true)] out string? triggerName)
    {
        triggerName = context.FunctionDefinition.InputBindings.Values
            .FirstOrDefault(b => b.Type.Equals(Constants.McpResourceTriggerBindingType, StringComparison.OrdinalIgnoreCase))
            ?.Name;

        return triggerName is not null;
    }
}
