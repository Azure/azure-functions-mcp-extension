// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


namespace Microsoft.Azure.Functions.Extensions.Mcp.Converters;

class StringConverter : IToolPropertyConverter
{
    public bool CanConvert(Type type) => type == typeof(string);
    public Task<object?> ConvertAsync(object? value, Type targetType) => Task.FromResult<object?>(value?.ToString() ?? string.Empty);
}
