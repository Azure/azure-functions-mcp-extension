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

    private static readonly byte[] HkdfEncInfo = "enc"u8.ToArray();
    private static readonly byte[] HkdfMacInfo = "mac"u8.ToArray();

    public static string ProtectUriState(string uriState, byte[]? key = null)
    {
        key ??= GetKey();
        var (encKey, macKey) = DeriveKeys(key);

        byte[] iv = GenerateIv();
        byte[] ciphertext = Encrypt(Encoding.UTF8.GetBytes(uriState), encKey, iv);
        byte[] payload = Concat(iv, ciphertext);
        byte[] signature = ComputeHmac(macKey, payload);

        return WebEncoders.Base64UrlEncode(Concat(payload, signature));
    }

    public static string ReadUriState(string token, byte[]? key = null)
    {
        key ??= GetKey();
        var (encKey, macKey) = DeriveKeys(key);

        byte[] tokenBytes = WebEncoders.Base64UrlDecode(token);
        var (iv, ciphertext, signature) = ParseToken(tokenBytes);

        // Verify HMAC over the payload (iv + ciphertext) using a slice to avoid redundant allocation
        int payloadLength = tokenBytes.Length - SignatureSize;
        VerifyHmac(macKey, tokenBytes.AsSpan(0, payloadLength), signature);

        return Encoding.UTF8.GetString(Decrypt(ciphertext, encKey, iv));
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

    private static (byte[] encKey, byte[] macKey) DeriveKeys(byte[] masterKey)
    {
        byte[] encKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, KeySize, info: HkdfEncInfo);
        byte[] macKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, KeySize, info: HkdfMacInfo);
        return (encKey, macKey);
    }

    private static byte[] GenerateIv()
    {
        byte[] iv = new byte[IvSize];
        RandomNumberGenerator.Fill(iv);
        return iv;
    }

    private static byte[] Encrypt(byte[] plaintext, byte[] key, byte[] iv)
    {
        using var aes = CreateAes(key, iv);
        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
    }

    private static byte[] Decrypt(byte[] ciphertext, byte[] key, byte[] iv)
    {
        using var aes = CreateAes(key, iv);
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    private static Aes CreateAes(byte[] key, byte[] iv)
    {
        var aes = Aes.Create();
        try
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }
        catch
        {
            aes.Dispose();
            throw;
        }
    }

    private static byte[] ComputeHmac(byte[] key, ReadOnlySpan<byte> data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data.ToArray());
    }

    private static void VerifyHmac(byte[] key, ReadOnlySpan<byte> data, byte[] expectedSignature)
    {
        byte[] computed = ComputeHmac(key, data);
        if (!CryptographicOperations.FixedTimeEquals(computed, expectedSignature))
        {
            throw new CryptographicException("Invalid token signature.");
        }
    }

    private static (byte[] iv, byte[] ciphertext, byte[] signature) ParseToken(byte[] tokenBytes)
    {
        const int minLength = IvSize + SignatureSize + 1;
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

        return (iv, ciphertext, signature);
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        byte[] result = new byte[a.Length + b.Length];
        Buffer.BlockCopy(a, 0, result, 0, a.Length);
        Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
        return result;
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
