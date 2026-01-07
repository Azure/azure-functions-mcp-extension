// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters.McpInputConversionHelper;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

internal class ToolInvocationPocoConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.TargetType.IsPoco()
            || context.TargetType == typeof(ToolInvocationContext)
            || !context.FunctionContext.TryGetToolInvocationContext(out ToolInvocationContext? toolContext))
        {
            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }

        if (toolContext is null)
        {
            return new ValueTask<ConversionResult>(
                ConversionResult.Failed(new InvalidOperationException($"{nameof(ToolInvocationContext)} was not available or was null in the current FunctionContext.")));
        }

        try
        {
            var poco = CreatePocoFromArguments(toolContext.Arguments!, context.TargetType);
            return new ValueTask<ConversionResult>(ConversionResult.Success(poco));
        }
        catch (Exception ex)
        {
            return new ValueTask<ConversionResult>(ConversionResult.Failed(ex));
        }
    }

    private object? CreatePocoFromArguments(IDictionary<string, object?> arguments, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(targetType);

        var poco = Activator.CreateInstance(targetType);

        foreach (var kvp in arguments)
        {
            var property = targetType.GetProperty(kvp.Key);
            if (property is null || !property.CanWrite)
            {
                continue;
            }

            try
            {
                if (TryConvertArgumentToTargetType(kvp.Value, property.PropertyType, out var convertedValue))
                {
                    property.SetValue(poco, convertedValue);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set property '{property.Name}'", ex);
            }
        }

        return poco;
    }
}
