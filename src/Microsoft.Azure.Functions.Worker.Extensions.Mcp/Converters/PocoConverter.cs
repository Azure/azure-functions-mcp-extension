// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

internal class PocoConverter : IInputConverter
{
    ValueTask<ConversionResult> IInputConverter.ConvertAsync(ConverterContext context) => ConvertAsync(context, CancellationToken.None);

    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context, CancellationToken cancellationToken = default)
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
        if (arguments is null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }

        if (targetType is null)
        {
            throw new ArgumentNullException(nameof(targetType));
        }

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
                var propertyTargetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                object? convertedValue;
                if (propertyTargetType.IsEnum)
                {
                    convertedValue = Enum.Parse(propertyTargetType, kvp.Value?.ToString()!);
                }
                else if (kvp.Value is IConvertible)
                {
                    convertedValue = Convert.ChangeType(kvp.Value, propertyTargetType);
                }
                else
                {
                    // Fallback for complex types
                    var json = JsonSerializer.Serialize(kvp.Value);
                    convertedValue = JsonSerializer.Deserialize(json, propertyTargetType);
                }

                property.SetValue(poco, convertedValue);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set property '{property.Name}'", ex);
            }
        }

        return poco;
    }
}
