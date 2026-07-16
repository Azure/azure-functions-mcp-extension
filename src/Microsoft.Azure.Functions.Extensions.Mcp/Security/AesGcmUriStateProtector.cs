// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Legacy AES-GCM protector (v0). Wire format: [iv12 | ciphertext | tag16 | signature32].
/// No version marker — this is the catch-all reader tried last.
/// </summary>
internal sealed class AesGcmUriStateProtector : IUriStateProtector
{
    private const int IvSize = 12;
    private const int TagSize = 16;
    private const int SignatureSize = 32;

    public int MinTokenLength => IvSize + TagSize + SignatureSize; // ciphertext can be 0 bytes (empty plaintext)

    public byte[] Protect(string uriState, byte[] key)
    {
        Span<byte> iv = stackalloc byte[IvSize];
        RandomNumberGenerator.Fill(iv);

        var plaintext = Encoding.UTF8.GetBytes(uriState);
        Span<byte> ciphertext = new byte[plaintext.Length];
        Span<byte> tag = stackalloc byte[TagSize];

        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Encrypt(iv, plaintext, ciphertext, tag);
        }

        int tokenLength = iv.Length + ciphertext.Length + tag.Length;
        Span<byte> tokenBytes = new byte[tokenLength];
        iv.CopyTo(tokenBytes[..iv.Length]);
        ciphertext.CopyTo(tokenBytes.Slice(iv.Length, ciphertext.Length));
        tag.CopyTo(tokenBytes.Slice(iv.Length + ciphertext.Length, tag.Length));

        Span<byte> signature = stackalloc byte[SignatureSize];
        HMACSHA256.TryHashData(key, tokenBytes, signature, out _);

        var finalToken = new byte[tokenBytes.Length + signature.Length];
        tokenBytes.CopyTo(finalToken);
        signature.CopyTo(finalToken.AsSpan(tokenLength, SignatureSize));

        return finalToken;
    }

    public bool TryRead(ReadOnlySpan<byte> token, byte[] key, [NotNullWhen(true)] out string? uriState)
    {
        uriState = null;

        if (token.Length < MinTokenLength)
        {
            return false;
        }

        int cipherLength = token.Length - IvSize - TagSize - SignatureSize;

        var iv = token[..IvSize];
        var ciphertext = token.Slice(IvSize, cipherLength);
        var tag = token.Slice(IvSize + cipherLength, TagSize);
        var signature = token.Slice(IvSize + cipherLength + TagSize, SignatureSize);

        var dataToSign = token[..(IvSize + cipherLength + TagSize)];

        Span<byte> computedSignature = stackalloc byte[SignatureSize];
        HMACSHA256.TryHashData(key, dataToSign, computedSignature, out _);

        if (!CryptographicOperations.FixedTimeEquals(signature, computedSignature))
        {
            return false;
        }

        try
        {
            Span<byte> plaintext = new byte[cipherLength];
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(iv, ciphertext, tag, plaintext);
            uriState = Encoding.UTF8.GetString(plaintext);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
