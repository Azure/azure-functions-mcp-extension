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
            bindingData,
            out var items);

        var nextCalled = false;
        await _middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

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
        var context = CreateFunctionContext(
            bindings: [],
            bindingData: [],
            out var items);

        var nextCalled = false;
        await _middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
        Assert.False(items.ContainsKey(Constants.ToolInvocationContextKey));
    }

    [Fact]
    public async Task Invoke_AddsResourceInvocationContext_WhenResourceTriggerPresent()
    {
        var triggerName = "myResourceTrigger";
        var resourceContext = new ResourceInvocationContext
        {
            Uri = "test://resource/1",
            SessionId = "session-123",
        };

        var bindingData = new Dictionary<string, object>
        {
            [triggerName] = JsonSerializer.Serialize(resourceContext)
        };

        var binding = CreateBindingMetadata(triggerName, Constants.McpResourceTriggerBindingType);
        var context = CreateFunctionContext([binding], bindingData, out var items);

        var nextCalled = false;
        await _middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
        Assert.True(items.ContainsKey(Constants.ResourceInvocationContextKey));

        var result = items[Constants.ResourceInvocationContextKey] as ResourceInvocationContext;

        Assert.NotNull(result);
        Assert.Equal("test://resource/1", result!.Uri);
        Assert.Equal("session-123", result.SessionId);
    }

    [Fact]
    public async Task Invoke_DoesNotAddResourceContext_WhenResourceTriggerMissing()
    {
        var context = CreateFunctionContext(
            bindings: [],
            bindingData: [],
            out var items);

        var nextCalled = false;
        await _middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
        Assert.False(items.ContainsKey(Constants.ResourceInvocationContextKey));
    }

    [Fact]
    public async Task Invoke_AddsBothContexts_WhenBothTriggersPresent()
    {
        var toolTriggerName = "myToolTrigger";
        var resourceTriggerName = "myResourceTrigger";

        var toolContext = new ToolInvocationContext
        {
            Name = "testTool",
            Arguments = new Dictionary<string, object> { ["foo"] = "bar" }
        };

        var resourceContext = new ResourceInvocationContext
        {
            Uri = "test://resource/1",
            SessionId = "session-123"
        };

        var bindingData = new Dictionary<string, object>
        {
            [toolTriggerName] = JsonSerializer.Serialize(toolContext),
            [resourceTriggerName] = JsonSerializer.Serialize(resourceContext)
        };

        var toolBinding = CreateBindingMetadata(toolTriggerName, Constants.McpToolTriggerBindingType);
        var resourceBinding = CreateBindingMetadata(resourceTriggerName, Constants.McpResourceTriggerBindingType);
        var context = CreateFunctionContext([toolBinding, resourceBinding], bindingData, out var items);

        var nextCalled = false;
        await _middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
        Assert.True(items.ContainsKey(Constants.ToolInvocationContextKey));
        Assert.True(items.ContainsKey(Constants.ResourceInvocationContextKey));

        var toolResult = items[Constants.ToolInvocationContextKey] as ToolInvocationContext;
        var resourceResult = items[Constants.ResourceInvocationContextKey] as ResourceInvocationContext;

        Assert.NotNull(toolResult);
        Assert.NotNull(resourceResult);
        Assert.Equal("testTool", toolResult!.Name);
        Assert.Equal("test://resource/1", resourceResult!.Uri);
    }

    [Fact]
    public async Task Invoke_HandlesInvalidResourceContextJson_Gracefully()
    {
        var triggerName = "myResourceTrigger";
        var bindingData = new Dictionary<string, object>
        {
            [triggerName] = "{invalid json"
        };

        var binding = CreateBindingMetadata(triggerName, Constants.McpResourceTriggerBindingType);
        var context = CreateFunctionContext([binding], bindingData, out var items);

        var exception = await Record.ExceptionAsync(async () =>
            await _middleware.Invoke(context, _ => Task.CompletedTask));

        Assert.NotNull(exception);
        Assert.IsType<JsonException>(exception);
    }

    [Fact]
    public async Task Invoke_ResourceContextWithMinimalData_WorksCorrectly()
    {
        var triggerName = "myResourceTrigger";
        var resourceContext = new ResourceInvocationContext
        {
            Uri = "test://minimal/resource"
        };

        var bindingData = new Dictionary<string, object>
        {
            [triggerName] = JsonSerializer.Serialize(resourceContext)
        };

        var binding = CreateBindingMetadata(triggerName, Constants.McpResourceTriggerBindingType);
        var context = CreateFunctionContext([binding], bindingData, out var items);

        var nextCalled = false;
        await _middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
        Assert.True(items.ContainsKey(Constants.ResourceInvocationContextKey));

        var result = items[Constants.ResourceInvocationContextKey] as ResourceInvocationContext;

        Assert.NotNull(result);
        Assert.Equal("test://minimal/resource", result!.Uri);
        Assert.Null(result.SessionId);
        Assert.Null(result.Transport);
    }

    [Fact]
    public async Task Invoke_ResourceContextWithTransportInfo_PreservesTransport()
    {
        var triggerName = "myResourceTrigger";
        var resourceContextJson = """
            {
                "uri": "test://resource/transport",
                "transport": {
                    "name": "http"
                }
            }
            """;

        var bindingData = new Dictionary<string, object>
        {
            [triggerName] = resourceContextJson
        };

        var binding = CreateBindingMetadata(triggerName, Constants.McpResourceTriggerBindingType);
        var context = CreateFunctionContext([binding], bindingData, out var items);

        var nextCalled = false;
        await _middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
        Assert.True(items.ContainsKey(Constants.ResourceInvocationContextKey));

        var result = items[Constants.ResourceInvocationContextKey] as ResourceInvocationContext;

        Assert.NotNull(result);
        Assert.NotNull(result!.Transport);
        Assert.Equal("http", result.Transport.Name);
    }

    [Fact]
    public async Task Invoke_DoesNotThrow_WhenNullContextPassed()
    {
        var exception = await Record.ExceptionAsync(async () =>
            await _middleware.Invoke(null!, _ => Task.CompletedTask));

        Assert.NotNull(exception);
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public async Task Invoke_SkipsResourceProcessing_WhenBindingDataMissingResourceTriggerValue()
    {
        var triggerName = "myResourceTrigger";
        var bindingData = new Dictionary<string, object>(); // No data for the trigger

        var binding = CreateBindingMetadata(triggerName, Constants.McpResourceTriggerBindingType);
        var context = CreateFunctionContext([binding], bindingData, out var items);

        var nextCalled = false;
        await _middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
        Assert.False(items.ContainsKey(Constants.ResourceInvocationContextKey));
    }

    [Fact]
    public async Task Invoke_ResourceContextDeserialization_PreservesAllProperties()
    {
        var triggerName = "myResourceTrigger";
        var resourceContext = new ResourceInvocationContext
        {
            Uri = "test://complex/resource",
            SessionId = "session-456",
        };

        var bindingData = new Dictionary<string, object>
        {
            [triggerName] = JsonSerializer.Serialize(resourceContext)
        };

        var binding = CreateBindingMetadata(triggerName, Constants.McpResourceTriggerBindingType);
        var context = CreateFunctionContext([binding], bindingData, out var items);

        var nextCalled = false;
        await _middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
        Assert.True(items.ContainsKey(Constants.ResourceInvocationContextKey));

        var result = items[Constants.ResourceInvocationContextKey] as ResourceInvocationContext;

        Assert.NotNull(result);
        Assert.Equal("test://complex/resource", result!.Uri);
        Assert.Equal("session-456", result.SessionId);
    }
}
