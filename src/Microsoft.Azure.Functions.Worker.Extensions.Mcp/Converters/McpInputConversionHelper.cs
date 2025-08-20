// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

internal static class McpInputConversionHelper
{
    public static bool TryConvertArgumentToTargetType(object? value, Type targetType, out object? result)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        try
        {
            return TryConvertArgumentToTargetTypeCore(value, targetType, out result);
        }
        catch
        {
            result = null;
            return false;
        }
    }

    private static bool TryConvertArgumentToTargetTypeCore(object? value, Type targetType, out object? result)
    {
        result = null;

        

        if (value is null)
        {
            return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null;
        }

        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (targetType.IsEnum)
        {
            if (value is int || value is long)
            {
                result = Enum.ToObject(targetType, value);
                return true;
            }

            return Enum.TryParse(targetType, value.ToString(), ignoreCase: true, out result);
        }

        if (value is IConvertible)
        {
            try
            {
                result = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception exc) when (exc is InvalidCastException || exc is FormatException) { }
        }

        var typeConverter = TypeDescriptor.GetConverter(targetType);
        if (typeConverter is not null && typeConverter.CanConvertFrom(value.GetType()))
        {
            result = typeConverter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
            return true;
        }

        result = value;
        return result is not null || !targetType.IsValueType;
    }
}
