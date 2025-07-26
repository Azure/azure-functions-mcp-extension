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
    /// Gets the name of the function with the <see cref="McpToolTriggerAttribute"/> from the <see cref="FunctionContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="FunctionContext"/>.</param>
    /// <param name="triggerName">The name of the MCP tool trigger.</param>
    /// <returns></returns>
    internal static bool TryGetMcpToolTriggerName(this FunctionContext context, out string triggerName)
    {
        foreach (var param in context.FunctionDefinition.Parameters)
        {
            if (param.Properties.TryGetValue(BindingAttribute, out var attr) &&
                attr?.GetType() == typeof(McpToolTriggerAttribute))
            {
                triggerName = param.Name;
                return true;
            }
        }

        triggerName = string.Empty;
        return false;
    }
}
