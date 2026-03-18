// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests;

public class McpAppConstantsTests : IDisposable
{
    public McpAppConstantsTests()
    {
        McpAppConstants.ClearForTesting();
    }

    public void Dispose()
    {
        McpAppConstants.ClearForTesting();
    }

    [Fact]
    public void SyntheticFunctionName_FormatsCorrectly()
    {
        var result = McpAppConstants.SyntheticFunctionName("data_explorer");

        Assert.Equal("__McpApp_data_explorer", result);
    }

    [Fact]
    public void IsSyntheticFunction_Registered_ReturnsTrue()
    {
        var name = McpAppConstants.SyntheticFunctionName("myTool");
        McpAppConstants.Register(name);

        Assert.True(McpAppConstants.IsSyntheticFunction(name));
    }

    [Fact]
    public void IsSyntheticFunction_NotRegistered_ReturnsFalse()
    {
        Assert.False(McpAppConstants.IsSyntheticFunction("__McpApp_unknown"));
    }

    [Fact]
    public void IsSyntheticFunction_SimilarPrefix_ReturnsFalse()
    {
        var name = McpAppConstants.SyntheticFunctionName("foo");
        McpAppConstants.Register(name);

        Assert.False(McpAppConstants.IsSyntheticFunction("__McpApp_foobar"));
    }

    [Fact]
    public void ExtractToolName_ReturnsCorrectName()
    {
        var syntheticName = McpAppConstants.SyntheticFunctionName("data_explorer");

        var result = McpAppConstants.ExtractToolName(syntheticName);

        Assert.Equal("data_explorer", result);
    }

    [Fact]
    public async Task Register_ThreadSafe_NoDuplicateException()
    {
        var name = McpAppConstants.SyntheticFunctionName("concurrent");

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => McpAppConstants.Register(name)))
            .ToArray();

        await Task.WhenAll(tasks);
        Assert.True(McpAppConstants.IsSyntheticFunction(name));
    }
}
