// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;


namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal class Utility
{
    private const string ValidChars = "abcdefghijklmnopqrstuvwxyz0123456789";
    private const int MaxByte = 252; // 252 is the largest multiple of 36 (the valid chars array) under 256
    private const int Length = 16;

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
}