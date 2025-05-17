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
    }
}