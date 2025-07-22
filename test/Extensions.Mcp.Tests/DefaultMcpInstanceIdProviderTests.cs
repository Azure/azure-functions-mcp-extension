// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class DefaultMcpInstanceIdProviderTests
{
    [Fact]
    public void InstanceId_IsNotNullOrEmpty()
    {
        var provider = new DefaultMcpInstanceIdProvider();

        var instanceId = provider.InstanceId;

        Assert.False(string.IsNullOrEmpty(instanceId));
    }

    [Fact]
    public void InstanceId_IsUnique()
    {
        var provider1 = new DefaultMcpInstanceIdProvider();
        var provider2 = new DefaultMcpInstanceIdProvider();

        Assert.NotEqual(provider1.InstanceId, provider2.InstanceId);
    }
}
