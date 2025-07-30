using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using static Worker.Extensions.Mcp.Tests.Helpers.FunctionContextHelper;

namespace Worker.Extensions.Mcp.Tests;

public class FunctionsMcpContextMiddlewareTests
{
    private readonly FunctionsMcpContextMiddleware _middleware = new();

    [Fact]
    public async Task Invoke_AddsToolInvocationContext_WhenTriggerPresent()
    {
        // Arrange
        var triggerName = "myTrigger";
        var toolContext = new ToolInvocationContext
        {
            Name = "testTool",
            Arguments = new Dictionary<string, object> { ["foo"] = "bar" }
        };

        var bindingData = new Dictionary<string, object>
        {
            [triggerName] = JsonSerializer.Serialize(toolContext)
        };

        var context = CreateFunctionContext(
            triggerName,
            new McpToolTriggerAttribute("tool"),
            bindingData,
            out var items);

        // Act
        var nextCalled = false;
        await _middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        // Assert
        Assert.True(nextCalled);
        Assert.True(items.ContainsKey(Constants.ToolInvocationContextKey));

        var result = items[Constants.ToolInvocationContextKey] as ToolInvocationContext;

        Assert.NotNull(result);
        Assert.Equal("testTool", result!.Name);
        Assert.Equal("bar", result.Arguments!["foo"].ToString());
    }

    [Fact]
    public async Task Invoke_DoesNotAddContext_WhenTriggerMissing()
    {
        // Arrange
        var context = CreateFunctionContext(
            parameters: [],
            bindingData: [],
            out var items);

        // Act
        var nextCalled = false;
        await _middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        // Assert
        Assert.True(nextCalled);
        Assert.False(items.ContainsKey(Constants.ToolInvocationContextKey));
    }
}
