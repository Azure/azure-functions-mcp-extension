// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters.TargetTypeConversionHelper;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

internal class ToolInvocationArgumentTypeConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.FunctionContext.TryGetToolInvocationContext(out ToolInvocationContext? toolContext)
            || toolContext.Arguments?.Count <= 0)
        {
            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }

        try
        {
            if (!context.TryGetBindingAttribute<BindingAttribute>(out object? bindingAttribute))
            {
                ArgumentNullException.ThrowIfNull(bindingAttribute, nameof(bindingAttribute));
            }

            var bindingKey = GetBindingKeyFromAttribute(bindingAttribute as BindingAttribute);
            var argument = toolContext.Arguments?.FirstOrDefault(a => a.Key.Equals(bindingKey, StringComparison.OrdinalIgnoreCase));

            if (TryConvertToTargetType(argument?.Value, context.TargetType, out var convertedValue))
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(convertedValue));
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
        catch (Exception ex)
        {
            return new ValueTask<ConversionResult>(ConversionResult.Failed(ex));
        }
    }

    private string? GetBindingKeyFromAttribute(BindingAttribute? bindingAttribute)
    {
        ArgumentNullException.ThrowIfNull(bindingAttribute);

        return bindingAttribute switch
        {
            McpToolPropertyAttribute a => a.PropertyName,
            McpToolTriggerAttribute a => a.ToolName,
            _ => null
        };
    }
}
