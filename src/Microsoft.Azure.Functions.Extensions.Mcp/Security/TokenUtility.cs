// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.WebUtilities;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Orchestrates token protection using a phased migration strategy.
/// Phase 1: Writes legacy (AES-GCM) format, reads both legacy and new (AES-CBC+HMAC) formats.
/// Phase 3 (future): Switch Writer to AesCbcHmacUriStateProtector.
/// </summary>
internal sealed class TokenUtility
{
    private const int KeySize = 32;

    // Phase 1: still emit legacy format for backward compatibility during rollout.
    private static readonly IUriStateProtector Writer = new AesGcmUriStateProtector();

    // Try new format first (fast version-byte check rejects non-matching tokens),
    // then fall back to legacy.
    private static readonly IUriStateProtector[] Readers =
    [
        new AesCbcHmacUriStateProtector(),
        new AesGcmUriStateProtector(),
    ];

    private static readonly int MinTokenLength = Readers.Min(r => r.MinTokenLength);

    public static string ProtectUriState(string uriState, byte[]? key = null)
    {
        key ??= GetKey();
        return WebEncoders.Base64UrlEncode(Writer.Protect(uriState, key));
    }

    public static string ReadUriState(string token, byte[]? key = null)
    {
        key ??= GetKey();
        byte[] tokenBytes = WebEncoders.Base64UrlDecode(token);

        if (tokenBytes.Length < MinTokenLength)
        {
            throw new InvalidOperationException("Token is too short or malformed.");
        }

        foreach (var reader in Readers)
        {
            if (reader.TryRead(tokenBytes, key, out var state))
            {
                return state;
            }
        }

        throw new CryptographicException("Invalid token signature.");
    }

    public static byte[] ToKeyBytes(string hexOrBase64)
    {
        // only support 32 bytes (256 bits) key length
        var keySpan = hexOrBase64.AsSpan();
        if (keySpan.Length == 64)
        {
            Span<byte> keyBytes = stackalloc byte[32];
            for (int i = 0; i < keyBytes.Length; i++)
            {
                keyBytes[i] = Convert.ToByte(keySpan.Slice(i * 2, 2).ToString(), 16);
            }
            return keyBytes.ToArray();
        }

        byte[] decoded = Convert.FromBase64String(hexOrBase64);
        if (decoded.Length != KeySize)
        {
            throw new ArgumentException($"Key must be exactly {KeySize} bytes, but got {decoded.Length} bytes.");
        }

        return decoded;
    }

    // Logic adapted from the Functions Host (https://github.com/Azure/azure-functions-host/blob/d1e06f1d9816105eefc4f50a78a1a43e63a936de/src/WebJobs.Script.WebHost/Security/SecretsUtility.cs?plain=1#L25C1-L25C96)
    public static bool TryGetEncryptionKey([NotNullWhen(true)] out string? key)
    {
        // Use WebSiteAuthEncryptionKey if available else fall back to ContainerEncryptionKey.
        // Until the container is specialized to a specific site WebSiteAuthEncryptionKey will not be available.
        if (TryGetEncryptionKey("WEBSITE_AUTH_ENCRYPTION_KEY", out key) ||
            TryGetEncryptionKey("CONTAINER_ENCRYPTION_KEY", out key))
        {
            return true;
        }

        return false;
    }

    private static byte[] GetKey()
    {
        if (TryGetEncryptionKey(out string? key))
        {
            return ToKeyBytes(key);
        }

        throw new InvalidOperationException("Encryption key not found.");
    }

    private static bool TryGetEncryptionKey(string environmentVariableName, [NotNullWhen(true)] out string? key)
    {
        key = Environment.GetEnvironmentVariable(environmentVariableName);

        return !string.IsNullOrEmpty(key);
    }
}
