// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

internal sealed class PromptArgumentConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return ConvertAsyncCore(context);
        }
        catch (Exception ex)
        {
            return new ValueTask<ConversionResult>(ConversionResult.Failed(ex));
        }
    }

    private static ValueTask<ConversionResult> ConvertAsyncCore(ConverterContext context)
    {
        if (context.FunctionContext.TryGetPromptInvocationContext(out var promptContext)
            && TryGetBindingAttribute(context, out var mcpBindingAttribute)
            && TryConvertArgument(promptContext, mcpBindingAttribute, out var convertedValue))
        {
            return new ValueTask<ConversionResult>(ConversionResult.Success(convertedValue));
        }

        return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
    }

    private static bool TryGetBindingAttribute(ConverterContext context, [NotNullWhen(true)] out IMcpBindingAttribute? mcpBindingAttribute)
    {
        mcpBindingAttribute = null;

        if (!context.TryGetBindingAttribute<BindingAttribute>(out var bindingAttribute)
            || bindingAttribute is not IMcpBindingAttribute mcpAttribute
            || mcpAttribute.BindingName is null)
        {
            return false;
        }

        mcpBindingAttribute = mcpAttribute;
        return true;
    }

    private static bool TryConvertArgument(PromptInvocationContext promptContext, IMcpBindingAttribute mcpBindingAttribute, out object? convertedValue)
    {
        convertedValue = null;

        if (promptContext.Arguments is null
            || !promptContext.Arguments.TryGetValue(mcpBindingAttribute.BindingName, out var argumentValue))
        {
            return false;
        }

        convertedValue = argumentValue;
        return true;
    }
}
