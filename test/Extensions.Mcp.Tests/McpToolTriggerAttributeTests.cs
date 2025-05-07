using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpToolTriggerAttributeTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var attribute = new McpToolTriggerAttribute("TestTool", "TestDescription");

        Assert.Equal("TestTool", attribute.ToolName);
        Assert.Equal("TestDescription", attribute.Description);
        Assert.Null(attribute.ToolProperties);
    }
}