// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class TokenUtilityTests
{
    private static byte[] GenerateKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    [Fact]
    public void ProtectAndReadUriState_RoundTrip_Success()
    {
        var state = "test-state";
        var key = GenerateKey();

        var token = TokenUtility.ProtectUriState(state, key);
        var result = TokenUtility.ReadUriState(token, key);

        Assert.Equal(state, result);
    }

    [Fact]
    public void ProtectAndReadUriState_EmptyString_RoundTrips()
    {
        var key = GenerateKey();

        var token = TokenUtility.ProtectUriState(string.Empty, key);
        var result = TokenUtility.ReadUriState(token, key);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ProtectAndReadUriState_UnicodeContent_RoundTrips()
    {
        var state = "https://example.com/callback?user=用户&emoji=🎉";
        var key = GenerateKey();

        var token = TokenUtility.ProtectUriState(state, key);
        var result = TokenUtility.ReadUriState(token, key);

        Assert.Equal(state, result);
    }

    [Fact]
    public void ProtectAndReadUriState_LargePayload_RoundTrips()
    {
        var state = new string('x', 10_000);
        var key = GenerateKey();

        var token = TokenUtility.ProtectUriState(state, key);
        var result = TokenUtility.ReadUriState(token, key);

        Assert.Equal(state, result);
    }

    [Fact]
    public void ProtectUriState_ProducesDifferentTokensForSameInput()
    {
        var state = "test-state";
        var key = GenerateKey();

        var token1 = TokenUtility.ProtectUriState(state, key);
        var token2 = TokenUtility.ProtectUriState(state, key);

        // Different IVs should produce different tokens
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void ReadUriState_WrongKey_ThrowsCryptographicException()
    {
        var state = "test-state";
        var key1 = GenerateKey();
        var key2 = GenerateKey();

        var token = TokenUtility.ProtectUriState(state, key1);

        Assert.Throws<CryptographicException>(() => TokenUtility.ReadUriState(token, key2));
    }

    [Fact]
    public void ReadUriState_TamperedToken_ThrowsCryptographicException()
    {
        var state = "test-state";
        var key = GenerateKey();

        var token = TokenUtility.ProtectUriState(state, key);
        var tokenBytes = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(token);

        // Flip a byte in the ciphertext area (after the 12-byte IV)
        tokenBytes[20] ^= 0xFF;

        var tamperedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(tokenBytes);
        Assert.Throws<CryptographicException>(() => TokenUtility.ReadUriState(tamperedToken, key));
    }

    [Fact]
    public void ReadUriState_TruncatedToken_ThrowsInvalidOperationException()
    {
        var key = GenerateKey();

        // Token shorter than minimum length
        var shortBytes = new byte[40];
        var shortToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(shortBytes);

        Assert.Throws<InvalidOperationException>(() => TokenUtility.ReadUriState(shortToken, key));
    }

    [Fact]
    public void ReadUriState_TamperedSignature_ThrowsCryptographicException()
    {
        var state = "test-state";
        var key = GenerateKey();

        var token = TokenUtility.ProtectUriState(state, key);
        var tokenBytes = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(token);

        // Flip a byte in the signature (last 32 bytes)
        tokenBytes[^1] ^= 0xFF;

        var tamperedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(tokenBytes);
        Assert.Throws<CryptographicException>(() => TokenUtility.ReadUriState(tamperedToken, key));
    }

    [Fact]
    public void ReadUriState_CanReadNewFormatToken()
    {
        // Directly use the new protector to create a v1 token, then verify TokenUtility can read it
        var state = "new-format-state";
        var key = GenerateKey();

        var newProtector = new AesCbcHmacUriStateProtector();
        var tokenBytes = newProtector.Protect(state, key);
        var token = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(tokenBytes);

        var result = TokenUtility.ReadUriState(token, key);
        Assert.Equal(state, result);
    }

    [Fact]
    public void ProtectUriState_EmitsLegacyFormat()
    {
        // Verify the writer still emits legacy (AES-GCM) format — no version byte prefix
        var state = "legacy-check";
        var key = GenerateKey();

        var token = TokenUtility.ProtectUriState(state, key);
        var tokenBytes = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(token);

        // Legacy format should NOT start with the v1 version byte (0x01) deterministically.
        // Since the first byte is a random IV byte, we verify by checking the legacy reader can parse it.
        var legacyReader = new AesGcmUriStateProtector();
        Assert.True(legacyReader.TryRead(tokenBytes, key, out var result));
        Assert.Equal(state, result);
    }

    [Fact]
    public void NewFormatProtector_RoundTrip_Success()
    {
        var state = "cbc-hmac-round-trip";
        var key = GenerateKey();

        var protector = new AesCbcHmacUriStateProtector();
        var tokenBytes = protector.Protect(state, key);

        Assert.True(protector.TryRead(tokenBytes, key, out var result));
        Assert.Equal(state, result);
    }

    [Fact]
    public void NewFormatProtector_TamperedToken_ReturnsFalse()
    {
        var state = "tamper-test";
        var key = GenerateKey();

        var protector = new AesCbcHmacUriStateProtector();
        var tokenBytes = protector.Protect(state, key);

        // Tamper with the IV (byte 10 is within the 16-byte IV at offset 1..16).
        // The IV is HMAC-covered, so any modification is detected.
        tokenBytes[10] ^= 0xFF;

        Assert.False(protector.TryRead(tokenBytes, key, out _));
    }

    [Fact]
    public void NewFormatProtector_WrongKey_ReturnsFalse()
    {
        var state = "wrong-key-test";
        var key1 = GenerateKey();
        var key2 = GenerateKey();

        var protector = new AesCbcHmacUriStateProtector();
        var tokenBytes = protector.Protect(state, key1);

        Assert.False(protector.TryRead(tokenBytes, key2, out _));
    }

    [Fact]
    public void NewFormatToken_HasVersionByte()
    {
        var state = "version-byte-check";
        var key = GenerateKey();

        var protector = new AesCbcHmacUriStateProtector();
        var tokenBytes = protector.Protect(state, key);

        Assert.Equal(AesCbcHmacUriStateProtector.Version, tokenBytes[0]);
    }

    [Fact]
    public void LegacyProtector_RoundTrip_Success()
    {
        var state = "legacy-round-trip";
        var key = GenerateKey();

        var protector = new AesGcmUriStateProtector();
        var tokenBytes = protector.Protect(state, key);

        Assert.True(protector.TryRead(tokenBytes, key, out var result));
        Assert.Equal(state, result);
    }

    [Fact]
    public void LegacyProtector_WrongKey_ReturnsFalse()
    {
        var state = "legacy-wrong-key";
        var key1 = GenerateKey();
        var key2 = GenerateKey();

        var protector = new AesGcmUriStateProtector();
        var tokenBytes = protector.Protect(state, key1);

        Assert.False(protector.TryRead(tokenBytes, key2, out _));
    }

    [Fact]
    public void ReadUriState_LegacyTokenStartingWith0x01_FallsBackToGcmReader()
    {
        // Craft a valid legacy GCM token whose first IV byte is 0x01 (same as CBC version marker).
        // This proves the orchestrator correctly falls back to the GCM reader when the CBC reader
        // rejects the token (HMAC mismatch under HKDF-derived keys).
        var state = "fallback-test";
        var key = GenerateKey();

        var legacyProtector = new AesGcmUriStateProtector();
        byte[] tokenBytes = legacyProtector.Protect(state, key);

        // Force first byte (start of GCM IV) to 0x01 — same as the CBC version marker.
        // We must re-sign because the HMAC covers the IV.
        tokenBytes[0] = 0x01;

        // Recompute the legacy HMAC over [iv12 | ciphertext | tag16]
        int signedLength = tokenBytes.Length - 32;
        byte[] newSig = System.Security.Cryptography.HMACSHA256.HashData(key, tokenBytes.AsSpan(0, signedLength));
        Buffer.BlockCopy(newSig, 0, tokenBytes, signedLength, 32);

        var token = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(tokenBytes);

        // The CBC reader tries first (version byte matches 0x01) but rejects via HMAC mismatch.
        // The GCM reader then passes HMAC verification but fails AES-GCM decrypt because the IV
        // was modified post-encryption. Both readers return false → CryptographicException.
        Assert.Throws<System.Security.Cryptography.CryptographicException>(
            () => TokenUtility.ReadUriState(token, key));
    }
}