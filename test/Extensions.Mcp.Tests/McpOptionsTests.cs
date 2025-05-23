// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpOptionsTests
{
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        var options = new McpOptions();

        Assert.Equal("Azure Functions MCP server", options.ServerName);
        Assert.Equal("1.0.0", options.ServerVersion);
        Assert.Null(options.Instructions);
        Assert.True(options.EncryptClientState);
        Assert.NotNull(options.MessageOptions);
        Assert.False(options.MessageOptions.UseAbsoluteUriForEndpoint);
    }
}