// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Utility class for common operations
/// </summary>
internal static class Utility
{
    /// <summary>
    /// Empty ID constant
    /// </summary>
    internal static readonly string EmptyId = string.Empty;

    /// <summary>
    /// Creates a new unique identifier
    /// </summary>
    /// <returns>A new GUID as a string</returns>
    internal static string CreateId()
    {
        return Guid.NewGuid().ToString();
    }
}
