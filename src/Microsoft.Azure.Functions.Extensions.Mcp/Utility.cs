// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;


namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal class Utility
{
    private const string ValidChars = "abcdefghijklmnopqrstuvwxyz0123456789";
    private const int MaxByte = 252; // 252 is the largest multiple of 36 (the valid chars array) under 256
    private const int Length = 16;
    private const string EmptyIdValue = "0000000000000000";

    public static string EmptyId => EmptyIdValue;

    internal static string CreateId()
    {
        Span<char> result = stackalloc char[Length];
        Span<byte> buffer = stackalloc byte[1];
        var count = 0;

        while (count < Length)
        {
            RandomNumberGenerator.Fill(buffer);
            var value = buffer[0];

            if (value < MaxByte)
            {
                result[count++] = ValidChars[value % ValidChars.Length];
            }
        }

        return new string(result);
    }

    /// <summary>
    /// Builds a nested JsonObject from flat key-value pairs where keys use "/" as path separator.
    /// For example, "ui/prefersBorder" with value true becomes { "ui": { "prefersBorder": true } }.
    /// Keys without "/" are added at the root level.
    /// </summary>
    internal static JsonObject? BuildNestedMetadataJson(IReadOnlyCollection<KeyValuePair<string, object?>> metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        var root = new JsonObject();

        foreach (var kvp in metadata)
        {
            var parts = kvp.Key.Split('/');
            var current = root;

            // Navigate/create nested objects for all parts except the last
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var part = parts[i];
                if (!current.ContainsKey(part))
                {
                    current[part] = new JsonObject();
                }

                current = current[part]!.AsObject();
            }

            // Set the value at the final key
            var finalKey = parts[^1];
            current[finalKey] = JsonValue.Create(kvp.Value);
        }

        return root;
    }
}
