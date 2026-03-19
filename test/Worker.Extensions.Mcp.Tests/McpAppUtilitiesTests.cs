// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests;

public class McpAppUtilitiesTests : IDisposable
{
    public McpAppUtilitiesTests()
    {
        McpAppUtilities.ClearForTesting();
    }

    public void Dispose()
    {
        McpAppUtilities.ClearForTesting();
    }

    [Fact]
    public void SyntheticFunctionName_FormatsCorrectly()
    {
        var result = McpAppUtilities.SyntheticFunctionName("data_explorer");

        Assert.Equal("functions--mcpapp-data_explorer", result);
    }

    [Fact]
    public void IsSyntheticFunction_Registered_ReturnsTrue()
    {
        var name = McpAppUtilities.SyntheticFunctionName("myTool");
        McpAppUtilities.Register(name);

        Assert.True(McpAppUtilities.IsSyntheticFunction(name));
    }

    [Fact]
    public void IsSyntheticFunction_NotRegistered_ReturnsFalse()
    {
        Assert.False(McpAppUtilities.IsSyntheticFunction("functions--mcpapp-unknown"));
    }

    [Fact]
    public void IsSyntheticFunction_SimilarPrefix_ReturnsFalse()
    {
        var name = McpAppUtilities.SyntheticFunctionName("foo");
        McpAppUtilities.Register(name);

        Assert.False(McpAppUtilities.IsSyntheticFunction("functions--mcpapp-foobar"));
    }

    [Fact]
    public void ExtractToolName_ReturnsCorrectName()
    {
        var syntheticName = McpAppUtilities.SyntheticFunctionName("data_explorer");

        var result = McpAppUtilities.ExtractToolName(syntheticName);

        Assert.Equal("data_explorer", result);
    }

    [Fact]
    public void ResourceUri_FormatsCorrectly()
    {
        var result = McpAppUtilities.ResourceUri("data_explorer");

        Assert.Equal("ui://data_explorer/view", result);
    }

    [Fact]
    public async Task Register_ThreadSafe_NoDuplicateException()
    {
        var name = McpAppUtilities.SyntheticFunctionName("concurrent");

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => McpAppUtilities.Register(name)))
            .ToArray();

        await Task.WhenAll(tasks);
        Assert.True(McpAppUtilities.IsSyntheticFunction(name));
    }
}
