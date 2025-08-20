// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

[SupportedTargetType(typeof(ToolInvocationContext))]
internal class ToolInvocationContextConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TargetType != typeof(ToolInvocationContext))
        {
            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }

        if (context.FunctionContext.TryGetToolInvocationContext(out var toolContext)
            && toolContext is not null)
        {
            return new ValueTask<ConversionResult>(ConversionResult.Success(toolContext));
        }

        var conversionError = $"{nameof(ToolInvocationContext)} was not available or was null in the current FunctionContext.";
        var conversionResult = ConversionResult.Failed(new InvalidOperationException(conversionError));
        return new ValueTask<ConversionResult>(conversionResult);
    }
}
