using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp;
using Xunit;

namespace Extensions.Mcp.Tests;

public class ToolPropertyTests
{
    [Fact]
    public void Required_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var attribute = new McpToolPropertyAttribute("name", "string", "description");
        
        // Assert
        Assert.False(attribute.Required);
    }
    
    [Fact]
    public void Required_WhenExplicitlySet_ReturnsCorrectValue()
    {
        // Arrange & Act
        var attribute = new McpToolPropertyAttribute("name", "string", "description", true);
        
        // Assert
        Assert.True(attribute.Required);
    }
}