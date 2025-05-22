// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class TokenUtilityTests_Extra
{
    [Fact]
    public void ToKeyBytes_HexAndBase64_Success()
    {
        var hex = new string('a', 64);
        var base64 = Convert.ToBase64String(new byte[32]);
        var hexBytes = TokenUtility.ToKeyBytes(hex);
        var base64Bytes = TokenUtility.ToKeyBytes(base64);
        Assert.Equal(32, hexBytes.Length);
        Assert.Equal(32, base64Bytes.Length);
    }

    [Fact]
    public void TryGetEncryptionKey_ReturnsFalse_WhenNotSet()
    {
        var original = Environment.GetEnvironmentVariable("WEBSITE_AUTH_ENCRYPTION_KEY");
        Environment.SetEnvironmentVariable("WEBSITE_AUTH_ENCRYPTION_KEY", null);
        try
        {
            Assert.False(TokenUtility.TryGetEncryptionKey(out _));
        }
        finally
        {
            Environment.SetEnvironmentVariable("WEBSITE_AUTH_ENCRYPTION_KEY", original);
        }
    }
}
