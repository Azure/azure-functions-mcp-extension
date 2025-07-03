// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp.Converters;

interface IToolPropertyConverter
{
    /// <summary>
    /// Can this converter turn `value` into <paramref name="targetType"/>?
    /// </summary>
    bool CanConvert(Type targetType);

    /// <summary>
    /// Convert the given `value` into the CLR object.
    /// </summary>
    Task<object?> ConvertAsync(object? value, Type targetType);
}
