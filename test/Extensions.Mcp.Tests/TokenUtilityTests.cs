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

        // Flip a byte in the ciphertext area (after the 16-byte IV)
        tokenBytes[20] ^= 0xFF;

        var tamperedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(tokenBytes);
        Assert.Throws<CryptographicException>(() => TokenUtility.ReadUriState(tamperedToken, key));
    }

    [Fact]
    public void ReadUriState_TruncatedToken_ThrowsInvalidOperationException()
    {
        var key = GenerateKey();

        // Token shorter than minimum (IvSize + SignatureSize + 1 = 49 bytes)
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
}