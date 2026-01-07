// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

internal static class McpInputConversionHelper
{
    private static readonly ConcurrentDictionary<Type, Func<int, IList>> _listFactoryCache = new();
    private static readonly ConcurrentDictionary<Type, Type> _elementTypeCache = new();

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

        try
        {
            result = ConvertArgumentToTargetType(value, targetType);
        }
        catch { }

        return result is not null || !targetType.IsValueType;
    }

    private static object? ConvertArgumentToTargetType(object? value, Type targetType)
    {
        if (value is null)
        {
            return null;
        }

        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // If the value is already of the target type, return it directly
        if (targetType.IsAssignableFrom(value.GetType()))
        {
            return value;
        }

        if (targetType.IsEnum)
        {
            if (value is int || value is long)
            {
                return Enum.ToObject(targetType, value);
            }

            return Enum.Parse(targetType, value.ToString()!, ignoreCase: true);
        }

        if (IsSupportedCollectionType(targetType) && value is List<object?> inputCollection)
        {
            return ConvertToCollection(inputCollection, targetType);
        }

        if (value is IConvertible)
        {
            try
            {
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception exc) when (exc is InvalidCastException || exc is FormatException) { }
        }

        var typeConverter = TypeDescriptor.GetConverter(targetType);
        if (typeConverter is not null && typeConverter.CanConvertFrom(value.GetType()))
        {
            return typeConverter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
        }

        // As a fallback, if the target type is a string, try ToString on the value
        if (targetType == typeof(string))
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        return null;
    }

    public static bool IsSupportedCollectionType(Type targetType)
    {
        return targetType != typeof(string)
            && (targetType.IsArray || targetType.IsAssignableTo(typeof(IEnumerable)));
    }

    public static object? ConvertToCollection(List<object?> source, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        if (source is null)
        {
            return null;
        }

        var elementType = GetElementType(targetType)
            ?? throw new InvalidOperationException($"Could not determine element type for {targetType}");

        if (targetType.IsArray)
        {
            var array = Array.CreateInstance(elementType, source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                var item = ConvertElementIfNeeded(source[i], elementType);
                array.SetValue(item, i);
            }

            return array;
        }

        var list = CreateTypedList(elementType, source.Count)!;
        var listType = list.GetType();

        foreach (var item in source)
        {
            var convertedItem = ConvertElementIfNeeded(item, elementType);
            list.Add(convertedItem);
        }

        if (targetType.IsAssignableFrom(listType))
        {
            return list;
        }

        // Attempt to convert to target type via constructor that accepts IEnumerable<T>
        var constructor = targetType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(elementType)]);
        if (constructor != null)
        {
            return constructor.Invoke([list]);
        }

        // Fall back to List<T>...
        return list;
    }

    private static IList CreateTypedList(Type elementType, int capacity)
    {
        var factory = _listFactoryCache.GetOrAdd(
            elementType,
            static t =>
            {
                var constructedListType = typeof(List<>).MakeGenericType(t);
                var ctor = constructedListType.GetConstructor(new[] { typeof(int) })!;
                return (int cap) => (IList)ctor.Invoke(new object[] { cap });
            });

        return factory(capacity);
    }

    private static object? ConvertElementIfNeeded(object? element, Type targetType)
    {
        if (element is null)
        {
            return null;
        }

        if (targetType.IsAssignableFrom(element.GetType()))
        {
            return element;
        }

        if (TryConvertArgumentToTargetType(element, targetType, out var converted))
        {
            return converted;
        }

        return element;
    }

    private static Type? GetElementType(Type collectionType)
    {
        return _elementTypeCache.GetOrAdd(collectionType, static t => GetElementTypeCore(t));

        static Type GetElementTypeCore(Type t)
        {
            if (t.IsArray)
            {
                return t.GetElementType()!;
            }

            if (MatchGenericEnumerable(t, null))
            {
                return t.GetGenericArguments()[0];
            }

            var enumerableType = t
                .FindInterfaces(MatchGenericEnumerable, null)
                .FirstOrDefault();

            if (enumerableType is not null)
            {
                return enumerableType.GetGenericArguments()[0];
            }

            return typeof(object);
        }

        static bool MatchGenericEnumerable(Type type, object? _)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
    }
}

