// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Abstraction for URI state token protection schemes.
/// Each implementation handles one wire format.
/// </summary>
internal interface IUriStateProtector
{
    int MinTokenLength { get; }

    byte[] Protect(string uriState, byte[] key);

    /// <summary>
    /// Attempts to read a token. Returns false (no throw) when the token
    /// isn't this format or fails authentication, so the caller can try another format.
    /// </summary>
    bool TryRead(ReadOnlySpan<byte> token, byte[] key, [NotNullWhen(true)] out string? uriState);
}
