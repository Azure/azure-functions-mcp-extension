// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class TokenUtilityTests
{
    [Fact]
    public void ProtectAndReadUriState_RoundTrip_Success()
    {
        var state = "test-state";
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);

        var token = TokenUtility.ProtectUriState(state, key);
        var result = TokenUtility.ReadUriState(token, key);

        Assert.Equal(state, result);
    }
}
