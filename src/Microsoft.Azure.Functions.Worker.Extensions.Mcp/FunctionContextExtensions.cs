// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Provides extension methods to work with <see cref="FunctionContext"/>.
/// </summary>
public static class FunctionContextExtensions
{
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
}
