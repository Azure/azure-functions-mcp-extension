// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

internal static class TargetTypeConversionHelper
{
    public static bool TryConvertToTargetType(object? value, Type targetType, out object? result)
    {
        result = null;

        try
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (value is null)
            {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null)
                {
                    // Cannot assign null to non-nullable value type
                    return false;
                }

                result = null;
                return true;
            }

            if (underlyingType.IsEnum)
            {
                result = Enum.Parse(underlyingType, value.ToString()!, ignoreCase: true);
                return true;
            }

            if (value is IConvertible)
            {
                result = Convert.ChangeType(value, underlyingType);
                return true;
            }

            var json = JsonSerializer.Serialize(value);
            result = JsonSerializer.Deserialize(json, underlyingType);
            return result is not null || !underlyingType.IsValueType;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}
