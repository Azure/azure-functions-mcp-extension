// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Provides extension methods to work with <see cref="FunctionContext"/>.
/// </summary>
internal static class FunctionContextExtensions
{
    /// <summary>
    /// Gets the <see cref="ToolInvocationContext"/> for the <see cref="FunctionContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="FunctionContext"/>.</param>
    /// <param name="toolContext">The <see cref="ToolInvocationContext"/> for the context.</param>
    /// <returns>true if the tool context was found; otherwise, false.</returns>
    internal static bool TryGetToolInvocationContext(this FunctionContext context, [NotNullWhen(true)] out ToolInvocationContext? toolContext)
    {
        return TryGetTypedContextItem(context, Constants.ToolInvocationContextKey, out toolContext);
    }

    /// <summary>
    /// Gets the <see cref="ResourceInvocationContext"/> for the <see cref="FunctionContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="FunctionContext"/>.</param>
    /// <param name="resourceContext">The <see cref="ResourceInvocationContext"/> for the context.</param>
    /// <returns>true if the resource context was found; otherwise, false.</returns>
    internal static bool TryGetResourceInvocationContext(this FunctionContext context, [NotNullWhen(true)] out ResourceInvocationContext? resourceContext)
    {
        return TryGetTypedContextItem(context, Constants.ResourceInvocationContextKey, out resourceContext);
    }

    /// <summary>
    /// Gets the name of the trigger binding with the <see cref="McpToolTriggerAttribute"/> from the <see cref="FunctionContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="FunctionContext"/>.</param>
    /// <param name="triggerName">The name of the MCP tool trigger.</param>
    /// <returns>true if the trigger name was found; otherwise, false.</returns>
    internal static bool TryGetMcpToolTriggerName(this FunctionContext context, [NotNullWhen(true)] out string? triggerName)
    {
        return TryGetMcpTriggerName<McpToolTriggerAttribute>(context, Constants.McpToolTriggerBindingType, out triggerName);
    }

    /// <summary>
    /// Gets the name of the trigger binding with the <see cref="McpResourceTriggerAttribute"/> from the <see cref="FunctionContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="FunctionContext"/>.</param>
    /// <param name="triggerName">The name of the MCP resource trigger.</param>
    /// <returns>true if the trigger name was found; otherwise, false.</returns>
    internal static bool TryGetMcpResourceTriggerName(this FunctionContext context, [NotNullWhen(true)] out string? triggerName)
    {
        return TryGetMcpTriggerName<McpResourceTriggerAttribute>(context, Constants.McpResourceTriggerBindingType, out triggerName);
    }

    /// <summary>
    /// Gets a typed item from the <see cref="FunctionContext"/> items dictionary.
    /// </summary>
    /// <typeparam name="T">The type of the item to retrieve.</typeparam>
    /// <param name="context">The <see cref="FunctionContext"/>.</param>
    /// <param name="key">The key of the item in the context items dictionary.</param>
    /// <param name="item">The typed item if found; otherwise, null.</param>
    /// <returns>true if the item was found and is of the expected type; otherwise, false.</returns>
    private static bool TryGetTypedContextItem<T>(this FunctionContext context, string key, [NotNullWhen(true)] out T? item) where T : class
    {
        item = null;

        if (context.Items.TryGetValue(key, out var value)
            && value is T typedItem)
        {
            item = typedItem;
        }

        return item is not null;
    }

    /// <summary>
    /// Gets the name of the trigger binding with the specified MCP trigger attribute type from the <see cref="FunctionContext"/>.
    /// </summary>
    /// <typeparam name="T">The type of the MCP trigger attribute.</typeparam>
    /// <param name="context">The <see cref="FunctionContext"/>.</param>
    /// <param name="bindingType">The binding type to search for.</param>
    /// <param name="triggerName">The name of the MCP trigger.</param>
    /// <returns>true if the trigger name was found; otherwise, false.</returns>
    private static bool TryGetMcpTriggerName<T>(this FunctionContext context, string bindingType, [NotNullWhen(true)] out string? triggerName) where T : Attribute
    {
        triggerName = context.FunctionDefinition.InputBindings.Values
            .FirstOrDefault(b => b.Type.Equals(bindingType, StringComparison.OrdinalIgnoreCase))
            ?.Name;

        return triggerName is not null;
    }
}
