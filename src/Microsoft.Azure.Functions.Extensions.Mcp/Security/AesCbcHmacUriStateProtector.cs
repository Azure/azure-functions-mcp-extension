// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// New AES-CBC + HMAC protector (v1). Wire format: [0x01 | iv16 | ciphertext | signature32].
/// Uses HKDF-derived subkeys for encryption and MAC. HMAC covers (version | iv | ciphertext).
/// </summary>
internal sealed class AesCbcHmacUriStateProtector : IUriStateProtector
{
    internal const byte Version = 0x01;
    private const int KeySize = 32;
    private const int IvSize = 16;
    private const int SignatureSize = 32;

    private static readonly byte[] HkdfEncInfo = "enc"u8.ToArray();
    private static readonly byte[] HkdfMacInfo = "mac"u8.ToArray();

    // version byte + IV + at least 1 block of ciphertext (16 for PKCS7) + signature
    public int MinTokenLength => 1 + IvSize + 16 + SignatureSize;

    public byte[] Protect(string uriState, byte[] key)
    {
        var (encKey, macKey) = DeriveKeys(key);

        byte[] iv = new byte[IvSize];
        RandomNumberGenerator.Fill(iv);

        byte[] ciphertext = Encrypt(Encoding.UTF8.GetBytes(uriState), encKey, iv);

        // payload = version | iv | ciphertext
        int payloadLength = 1 + iv.Length + ciphertext.Length;
        byte[] payload = new byte[payloadLength];
        payload[0] = Version;
        Buffer.BlockCopy(iv, 0, payload, 1, iv.Length);
        Buffer.BlockCopy(ciphertext, 0, payload, 1 + iv.Length, ciphertext.Length);

        byte[] signature = HMACSHA256.HashData(macKey, payload);

        byte[] result = new byte[payloadLength + SignatureSize];
        Buffer.BlockCopy(payload, 0, result, 0, payloadLength);
        Buffer.BlockCopy(signature, 0, result, payloadLength, SignatureSize);

        return result;
    }

    public bool TryRead(ReadOnlySpan<byte> token, byte[] key, [NotNullWhen(true)] out string? uriState)
    {
        uriState = null;

        if (token.Length < MinTokenLength)
        {
            return false;
        }

        // Check version marker
        if (token[0] != Version)
        {
            return false;
        }

        var (encKey, macKey) = DeriveKeys(key);

        int payloadLength = token.Length - SignatureSize;
        var payload = token[..payloadLength];
        var signature = token[payloadLength..];

        // Verify HMAC
        Span<byte> computedSignature = stackalloc byte[SignatureSize];
        HMACSHA256.TryHashData(macKey, payload, computedSignature, out _);

        if (!CryptographicOperations.FixedTimeEquals(signature, computedSignature))
        {
            return false;
        }

        // Parse: skip version byte, then IV, then ciphertext
        var iv = payload.Slice(1, IvSize);
        var ciphertext = payload.Slice(1 + IvSize);

        try
        {
            byte[] plaintext = Decrypt(ciphertext.ToArray(), encKey, iv.ToArray());
            uriState = Encoding.UTF8.GetString(plaintext);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static (byte[] encKey, byte[] macKey) DeriveKeys(byte[] masterKey)
    {
        byte[] encKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, KeySize, info: HkdfEncInfo);
        byte[] macKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, KeySize, info: HkdfMacInfo);
        return (encKey, macKey);
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
}
