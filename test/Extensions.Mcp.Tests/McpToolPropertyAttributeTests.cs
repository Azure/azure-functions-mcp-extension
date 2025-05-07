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
    }
}