// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class UtilityTests
{
    [Fact]
    public void CreateId_GeneratesUniqueId()
    {
        var id1 = Utility.CreateId();
        var id2 = Utility.CreateId();

        Assert.False(string.IsNullOrEmpty(id1));
        Assert.False(string.IsNullOrEmpty(id2));
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void CreateId_UsesOnlyValidCharacters()
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var id = Utility.CreateId();

        Assert.All(id, c => Assert.Contains(c, validChars));
    }
}
