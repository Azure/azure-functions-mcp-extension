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

        // Check if target type is an array or collection
        if (IsArrayOrCollection(targetType))
        {
            return TryConvertToArrayOrCollection(value, targetType, out result);
        }

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

    private static bool IsArrayOrCollection(Type type)
    {
        // Exclude string - it implements IEnumerable<char> but we don't want to treat it as a collection
        if (type == typeof(string))
            return false;

        // Check if it's an array
        if (type.IsArray)
            return true;

        // Check if it implements IEnumerable<T>
        return type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    private static bool TryConvertToArrayOrCollection(object? value, Type targetType, out object? result)
    {
        result = null;

        if (value is null)
            return true;

        // Get the element type
        Type? elementType = GetElementType(targetType);
        if (elementType is null)
            return false;

        if (value is string)
        {
            return false;
        }

        // Convert the value to a collection
        if (value is IEnumerable<object> enumerable)
        {
            var list = new List<object?>();

            foreach (var item in enumerable)
            {
                if (TryConvertArgumentToTargetTypeCore(item, elementType, out object? convertedItem))
                {
                    list.Add(convertedItem);
                }
                else
                {
                    return false; // Failed to convert an item
                }
            }

            // Create the target collection type
            result = CreateCollectionOfType(list, targetType, elementType);
            return result is not null;
        }

        return false;
    }

    private static Type? GetElementType(Type collectionType)
    {
        // Handle arrays (string[], int[], etc.)
        if (collectionType.IsArray)
            return collectionType.GetElementType();

        // Handle generic collections (List<T>, IEnumerable<T>, etc.)
        if (collectionType.IsGenericType)
        {
            var genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length == 1)
                return genericArgs[0];
        }

        // Handle types that implement IEnumerable<T>
        var enumerableInterface = collectionType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments()[0];
    }

    private static object? CreateCollectionOfType(List<object?> items, Type targetType, Type elementType)
    {
        // Create typed array first
        var typedArray = Array.CreateInstance(elementType, items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            typedArray.SetValue(items[i], i);
        }

        // If target is an array, return the array
        if (targetType.IsArray)
            return typedArray;

        // If target is a generic collection type, convert accordingly
        if (targetType.IsGenericType)
        {
            var genericTypeDef = targetType.GetGenericTypeDefinition();

            if (genericTypeDef == typeof(List<>))
            {
                var listType = typeof(List<>).MakeGenericType(elementType);
                var constructor = listType.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(elementType) });
                return constructor?.Invoke(new[] { typedArray });
            }
            else if (genericTypeDef == typeof(IList<>) ||
                     genericTypeDef == typeof(ICollection<>) ||
                     genericTypeDef == typeof(IEnumerable<>))
            {
                // For interfaces, return a List<T>
                var listType = typeof(List<>).MakeGenericType(elementType);
                var constructor = listType.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(elementType) });
                return constructor?.Invoke(new[] { typedArray });
            }
            else if (genericTypeDef == typeof(HashSet<>))
            {
                var hashSetType = typeof(HashSet<>).MakeGenericType(elementType);
                var constructor = hashSetType.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(elementType) });
                return constructor?.Invoke(new[] { typedArray });
            }
        }

        // Fallback: return the array
        return typedArray;
    }
}
