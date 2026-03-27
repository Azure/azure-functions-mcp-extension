// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests;

public class McpAppUtilitiesTests
{
    [Fact]
    public void SyntheticFunctionName_FormatsCorrectly()
    {
        var result = McpAppUtilities.SyntheticFunctionName("data_explorer");

        Assert.Equal("functions--mcpapp-data_explorer", result);
    }

    [Fact]
    public void IsSyntheticFunction_MatchingPrefix_ReturnsTrue()
    {
        var name = McpAppUtilities.SyntheticFunctionName("myTool");

        Assert.True(McpAppUtilities.IsSyntheticFunction(name));
    }

    [Fact]
    public void IsSyntheticFunction_NonMatchingName_ReturnsFalse()
    {
        Assert.False(McpAppUtilities.IsSyntheticFunction("RegularFunction"));
    }

    [Fact]
    public void IsSyntheticFunction_EmptyString_ReturnsFalse()
    {
        Assert.False(McpAppUtilities.IsSyntheticFunction(string.Empty));
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
}
