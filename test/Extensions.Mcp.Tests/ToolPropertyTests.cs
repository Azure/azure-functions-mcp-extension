using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class ToolPropertyTests
{
    [Fact]
    public void IsRequired_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var attribute = new McpToolPropertyAttribute("name", "string", "description");

        // Assert
        Assert.False(attribute.IsRequired);
    }

    [Fact]
    public void IsRequired_WhenExplicitlySet_ReturnsCorrectValue()
    {
        // Arrange & Act
        var attribute = new McpToolPropertyAttribute("name", "string", "description", true);

        // Assert
        Assert.True(attribute.IsRequired);
    }
}
