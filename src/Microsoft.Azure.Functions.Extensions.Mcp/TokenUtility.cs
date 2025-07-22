// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.WebUtilities;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class TokenUtility
{
    private const int KeySize = 32;
    private const int IvSize = 12;
    private const int TagSize = 16;
    private const int SignatureSize = 32;

    public static string ProtectUriState(string uriState, byte[]? key = null)
    {
        key ??= GetKey();

        Span<byte> iv = stackalloc byte[IvSize];
        RandomNumberGenerator.Fill(iv);

        var plaintext = Encoding.UTF8.GetBytes(uriState);
        Span<byte> ciphertext = new byte[plaintext.Length]; // Allocate on the heap here
        Span<byte> tag = stackalloc byte[TagSize];

        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Encrypt(iv, plaintext, ciphertext, tag);
        }

        // Compute HMAC-SHA256 to sign the token
        int tokenLength = iv.Length + ciphertext.Length + tag.Length;
        Span<byte> tokenBytes = new byte[tokenLength];
        iv.CopyTo(tokenBytes[..iv.Length]);
        ciphertext.CopyTo(tokenBytes.Slice(iv.Length, ciphertext.Length));
        tag.CopyTo(tokenBytes.Slice(iv.Length + ciphertext.Length, tag.Length));

        Span<byte> signature = stackalloc byte[SignatureSize];
        using (var hmac = new HMACSHA256(key))
        {
            hmac.TryComputeHash(tokenBytes, signature, out _);
        }

        // Encode for URL use
        var finalToken = new byte[tokenBytes.Length + signature.Length];
        tokenBytes.CopyTo(finalToken);
        signature.CopyTo(finalToken.AsSpan(tokenLength, SignatureSize));

        return WebEncoders.Base64UrlEncode(finalToken);
    }

    public static string ReadUriState(string token, byte[]? key = null)
    {
        key ??= GetKey();

        ReadOnlySpan<byte> tokenSpan = WebEncoders.Base64UrlDecode(token);

        const int minLength = IvSize + TagSize + SignatureSize + 1; // at least 1 byte of ciphertext

        if (tokenSpan.Length < minLength)
        {
            throw new InvalidOperationException("Token is too short or malformed.");
        }

        int cipherLength = tokenSpan.Length - IvSize - TagSize - SignatureSize;

        var iv = tokenSpan[..IvSize];
        var ciphertext = tokenSpan.Slice(IvSize, cipherLength);
        var tag = tokenSpan.Slice(IvSize + cipherLength, TagSize);
        var signature = tokenSpan.Slice(IvSize + cipherLength + TagSize, SignatureSize);

        var dataToSign = tokenSpan[..(IvSize + cipherLength + TagSize)];

        Span<byte> computedSignature = stackalloc byte[SignatureSize];
        using (var hmac = new HMACSHA256(key))
        {
            hmac.TryComputeHash(dataToSign, computedSignature, out _);
        }

        if (!CryptographicOperations.FixedTimeEquals(signature, computedSignature))
        {
            throw new CryptographicException("Invalid token signature.");
        }

        Span<byte> plaintext = stackalloc byte[cipherLength];
        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Decrypt(iv, ciphertext, tag, plaintext);
        }

        return Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] GetKey()
    {
        if (TryGetEncryptionKey(out string? key))
        {
            return ToKeyBytes(key);
        }

        throw new InvalidOperationException("Encryption key not found.");
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

    private static bool TryGetEncryptionKey(string environmentVariableName, [NotNullWhen(true)] out string? key)
    {
        key = Environment.GetEnvironmentVariable(environmentVariableName);

        return !string.IsNullOrEmpty(key);
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

        return Convert.FromBase64String(hexOrBase64);
    }
}
