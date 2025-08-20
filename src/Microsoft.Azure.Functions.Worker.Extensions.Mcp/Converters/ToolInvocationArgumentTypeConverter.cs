// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using System.Diagnostics.CodeAnalysis;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters.McpInputConversionHelper;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

internal class ToolInvocationArgumentTypeConverter : IInputConverter
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
        if (context.FunctionContext.TryGetToolInvocationContext(out var toolContext)
            && TryGetBindingAttribute(context, out var mcpBindingAttribute)
            && TryConvertArgument(toolContext, mcpBindingAttribute, context.TargetType, out var convertedValue))

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

    private static bool TryConvertArgument(ToolInvocationContext toolContext, IMcpBindingAttribute mcpBindingAttribute, Type targetType, out object? convertedValue)
    {
        convertedValue = null;

        if (toolContext.Arguments is null
            || !toolContext.Arguments.TryGetValue(mcpBindingAttribute.BindingName, out var argumentValue))
        {
            return false;
        }

        return TryConvertArgumentToTargetType(argumentValue, targetType, out convertedValue);
    }
}
