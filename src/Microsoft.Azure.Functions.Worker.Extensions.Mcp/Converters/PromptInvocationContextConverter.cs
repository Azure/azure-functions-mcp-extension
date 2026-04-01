// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

internal sealed class PromptInvocationContextConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TargetType != typeof(PromptInvocationContext))
        {
            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }

        if (context.FunctionContext.TryGetPromptInvocationContext(out var promptContext))
        {
            return new ValueTask<ConversionResult>(ConversionResult.Success(promptContext));
        }

        var conversionError = $"{nameof(PromptInvocationContext)} was not available or was null in the current FunctionContext.";
        var conversionResult = ConversionResult.Failed(new InvalidOperationException(conversionError));

        return new ValueTask<ConversionResult>(conversionResult);
    }
}
