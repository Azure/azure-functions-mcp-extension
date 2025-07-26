// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

internal class ToolInvocationContextConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        object? target = null;

        if (context.TargetType == typeof(ToolInvocationContext)
            && context.FunctionContext.TryGetToolInvocationContext(out var toolContext))
        {
            target = toolContext;
        }

        if (target is not null)
        {
            return new ValueTask<ConversionResult>(ConversionResult.Success(target));
        }

        return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
    }
}
