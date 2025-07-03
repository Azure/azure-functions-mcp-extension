// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Converters;

static class ToolPropertyConverterRegistry
{
    private static readonly List<IToolPropertyConverter> _converters =
        new() {
            new StringConverter(),
            new JsonPocoConverter()
        };

    public static async Task<object?> ToTargetTypeAsync(object? rawValue, Type targetType)
    {
        var conv = _converters.First(c => c.CanConvert(targetType));
        return await conv.ConvertAsync(rawValue, targetType);
    }
}
