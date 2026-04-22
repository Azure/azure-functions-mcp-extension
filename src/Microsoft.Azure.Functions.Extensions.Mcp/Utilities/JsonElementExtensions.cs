// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal static class JsonElementExtensions
{
    /// <summary>
    /// Checks if a JSON element represents a null or undefined value.
    /// </summary>
    /// <param name="value">The JSON element to check.</param>
    /// <returns>True if the value is null or undefined; otherwise false.</returns>
    public static bool IsNullOrUndefined(this JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => true,
        JsonValueKind.Undefined => true,
        _ => false,
    };
}
