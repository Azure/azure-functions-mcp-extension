// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Backplane;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpBackplaneMessageTests
{
    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var dummy = new JsonRpcRequest {Method = "test"};
        var msg = new McpBackplaneMessage
        {
            ClientId = "client",
            Message = dummy
        };
        Assert.Equal("client", msg.ClientId);
        Assert.Same(dummy, msg.Message);
    }
}
