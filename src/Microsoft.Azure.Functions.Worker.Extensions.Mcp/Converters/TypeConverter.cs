// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

internal class TypeConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Only expecting a single argument, otherwise use a different converter
        if (!context.FunctionContext.TryGetToolInvocationContext(out ToolInvocationContext? toolContext)
            || toolContext.Arguments?.Count != 1)
        {
            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }

        object? argValue = toolContext.Arguments.Values.FirstOrDefault();

        if (argValue is JsonElement jsonElement)
        {
            var conversionResult = GetJsonElementConversion(jsonElement, context.TargetType);
            return new ValueTask<ConversionResult>(conversionResult);
        }

        if (argValue is not null && context.TargetType.IsAssignableFrom(argValue.GetType()))
        {
            return new ValueTask<ConversionResult>(ConversionResult.Success(argValue));
        }

        return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
    }

    private ConversionResult GetJsonElementConversion(JsonElement jsonElement, Type targetType) => jsonElement.ValueKind switch
    {
        JsonValueKind.String when targetType == typeof(string) =>
            ConversionResult.Success(jsonElement.GetString()),

        JsonValueKind.Number when targetType == typeof(int) && jsonElement.TryGetInt32(out var i) =>
            ConversionResult.Success(i),

        JsonValueKind.Number when targetType == typeof(long) && jsonElement.TryGetInt64(out var l) =>
            ConversionResult.Success(l),

        JsonValueKind.Number when targetType == typeof(float) && jsonElement.TryGetSingle(out var f) =>
            ConversionResult.Success(f),

        JsonValueKind.Number when targetType == typeof(double) && jsonElement.TryGetDouble(out var d) =>
            ConversionResult.Success(d),

        JsonValueKind.Number when targetType == typeof(decimal) && jsonElement.TryGetDecimal(out var m) =>
            ConversionResult.Success(m),

        JsonValueKind.True when targetType == typeof(bool) =>
            ConversionResult.Success(true),

        JsonValueKind.False when targetType == typeof(bool) =>
            ConversionResult.Success(false),

        JsonValueKind.Null when !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null =>
            ConversionResult.Success(null),

        _ => ConversionResult.Unhandled()
    };
}

