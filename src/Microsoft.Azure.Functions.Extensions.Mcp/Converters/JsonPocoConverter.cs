// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Converters;

class JsonPocoConverter : IToolPropertyConverter
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool CanConvert(Type type) => IsPoco(type);

    public Task<object?> ConvertAsync(object? value, Type type) => value switch
    {
        JsonElement jsonElement => Task.FromResult(jsonElement.Deserialize(type, _serializerOptions)),
        string str => Task.FromResult(JsonSerializer.Deserialize(str, type, _serializerOptions)),
        byte[] bytes => Task.FromResult(JsonSerializer.Deserialize(bytes, type, _serializerOptions)),
        Stream stream => JsonSerializer.DeserializeAsync(stream, type, _serializerOptions).AsTask(),
        _ => throw new ArgumentException(
                $"Cannot convert value of type {value?.GetType().Name} to type {type.Name}. " +
                "Expected a JsonElement, string, byte array, or Stream.")
    };

    /// <summary>
    /// Checks if the given type qualifies as a POCO for JSON deserialization.
    /// Excludes:
    /// - string
    /// - abstract types and interfaces
    /// - collection types (IEnumerable)
    /// - types without a public parameterless constructor
    /// </summary>
    private static bool IsPoco(Type type)
    {
        if (type == typeof(string))
            return false;

        if (type.IsAbstract || type.IsInterface)
            return false;

        if (typeof(IEnumerable).IsAssignableFrom(type))
            return false;

        if (!type.IsClass)
            return false;

        if (type.GetConstructor(Type.EmptyTypes) == null)
            return false;

        return true;
    }
}
