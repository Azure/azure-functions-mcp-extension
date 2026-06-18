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
    private const int IvSize = 16; // AES block size for CBC
    private const int SignatureSize = 32;

    public static string ProtectUriState(string uriState, byte[]? key = null)
    {
        key ??= GetKey();

        byte[] iv = new byte[IvSize];
        RandomNumberGenerator.Fill(iv);

        byte[] ciphertext;
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var plaintext = Encoding.UTF8.GetBytes(uriState);
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        // Compute HMAC-SHA256 over IV + ciphertext for integrity
        int dataLength = iv.Length + ciphertext.Length;
        byte[] tokenBytes = new byte[dataLength];
        Buffer.BlockCopy(iv, 0, tokenBytes, 0, iv.Length);
        Buffer.BlockCopy(ciphertext, 0, tokenBytes, iv.Length, ciphertext.Length);

        byte[] signature;
        using (var hmac = new HMACSHA256(key))
        {
            signature = hmac.ComputeHash(tokenBytes);
        }

        // Encode for URL use
        var finalToken = new byte[tokenBytes.Length + signature.Length];
        Buffer.BlockCopy(tokenBytes, 0, finalToken, 0, tokenBytes.Length);
        Buffer.BlockCopy(signature, 0, finalToken, tokenBytes.Length, signature.Length);

        return WebEncoders.Base64UrlEncode(finalToken);
    }

    public static string ReadUriState(string token, byte[]? key = null)
    {
        key ??= GetKey();

        byte[] tokenBytes = WebEncoders.Base64UrlDecode(token);

        const int minLength = IvSize + SignatureSize + 1; // at least 1 byte of ciphertext

        if (tokenBytes.Length < minLength)
        {
            throw new InvalidOperationException("Token is too short or malformed.");
        }

        int cipherLength = tokenBytes.Length - IvSize - SignatureSize;

        byte[] iv = new byte[IvSize];
        Buffer.BlockCopy(tokenBytes, 0, iv, 0, IvSize);

        byte[] ciphertext = new byte[cipherLength];
        Buffer.BlockCopy(tokenBytes, IvSize, ciphertext, 0, cipherLength);

        byte[] signature = new byte[SignatureSize];
        Buffer.BlockCopy(tokenBytes, IvSize + cipherLength, signature, 0, SignatureSize);

        // Verify HMAC-SHA256 signature
        byte[] dataToSign = new byte[IvSize + cipherLength];
        Buffer.BlockCopy(tokenBytes, 0, dataToSign, 0, IvSize + cipherLength);

        byte[] computedSignature;
        using (var hmac = new HMACSHA256(key))
        {
            computedSignature = hmac.ComputeHash(dataToSign);
        }

        if (!CryptographicOperations.FixedTimeEquals(signature, computedSignature))
        {
            throw new CryptographicException("Invalid token signature.");
        }

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        byte[] plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

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