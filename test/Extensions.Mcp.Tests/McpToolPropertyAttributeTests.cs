// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpToolPropertyAttributeTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var attribute = new McpToolPropertyAttribute("name", "string", "description");

        Assert.Equal("name", attribute.PropertyName);
        Assert.Equal("string", attribute.PropertyType);
        Assert.Equal("description", attribute.Description);
        Assert.False(attribute.Required); // Default value should be false
    }

    [Fact]
    public void Constructor_WithRequired_SetsPropertiesCorrectly()
    {
        var attribute = new McpToolPropertyAttribute("name", "string", "description", true);

        Assert.Equal("name", attribute.PropertyName);
        Assert.Equal("string", attribute.PropertyType);
        Assert.Equal("description", attribute.Description);
        Assert.True(attribute.Required);
    }
}
