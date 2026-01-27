// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpResourceTriggerBindingTests
{
    private static (McpResourceTriggerBinding binding, ParameterInfo parameter) CreateBinding(
        ParameterInfo? parameter = null,
        string uri = "test://resource/1",
        string resourceName = "TestResource")
    {
        if (parameter is null)
        {
            var method = typeof(McpResourceTriggerBindingTests).GetMethod(nameof(DummyMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
            parameter = method.GetParameters()[0];
        }

        var resourceRegistry = new Mock<IResourceRegistry>();
        var attribute = new McpResourceTriggerAttribute(uri, resourceName)
        {
            Description = "Test resource",
            MimeType = "text/plain"
        };

        var binding = new McpResourceTriggerBinding(parameter, resourceRegistry.Object, attribute, NullLoggerFactory.Instance);

        return (binding, parameter);
    }

    private static void DummyMethod([McpResourceTrigger("test://resource/1", "TestResource")] ResourceInvocationContext ctx) { }

    private static void DummyStringMethod([McpResourceTrigger("test://resource/1", "TestResource")] string ctx) { }

    private static void DummyMethodWithMetadata(
        [McpResourceTrigger("test://resource/1", "TestResource")]
        ResourceInvocationContext ctx) { }

    private static void DummyMethodWithoutMetadata([McpResourceTrigger("test://resource/1", "TestResource")] ResourceInvocationContext ctx) { }

    private static ValueBindingContext CreateValueBindingContext()
    {
        var functionContext = new FunctionBindingContext(Guid.NewGuid(), CancellationToken.None);
        return new ValueBindingContext(functionContext, CancellationToken.None);
    }

    [Fact]
    public void Constructor_SetsBindingDataContract()
    {
        var (binding, param) = CreateBinding();

        Assert.NotNull(binding.BindingDataContract);
        Assert.True(binding.BindingDataContract.ContainsKey(param.Name!));
        Assert.True(binding.BindingDataContract.ContainsKey("mcpresourceuri"));
        Assert.True(binding.BindingDataContract.ContainsKey("mcpresourcecontext"));
        Assert.True(binding.BindingDataContract.ContainsKey("mcpsessionid"));
        Assert.True(binding.BindingDataContract.ContainsKey("$return"));
    }

    [Fact]
    public async Task BindAsync_BasicBinding_PopulatesBindingData()
    {
        var (binding, param) = CreateBinding();
        var executionContext = ReadResourceExecutionContextHelper.CreateExecutionContext();
        var triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        Assert.True(triggerData.BindingData.ContainsKey("mcpresourcecontext"));
        Assert.True(triggerData.BindingData.ContainsKey(param.Name!));
        Assert.True(triggerData.BindingData.ContainsKey("mcpresourceuri"));
        Assert.True(triggerData.BindingData.ContainsKey("mcpsessionid"));

        var ctx = Assert.IsType<ResourceInvocationContext>(triggerData.BindingData["mcpresourcecontext"]);
        Assert.Equal("test://resource/1", ctx.Uri);
        Assert.Equal("session-123", ctx.SessionId);
    }

    [Fact]
    public async Task BindAsync_StringParameter_SerializesContext()
    {
        var method = typeof(McpResourceTriggerBindingTests).GetMethod(nameof(DummyStringMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var param = method.GetParameters()[0];

        var (binding, _) = CreateBinding(param);
        var executionContext = ReadResourceExecutionContextHelper.CreateExecutionContext();
        var triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        var serialized = Assert.IsType<string>(triggerData.BindingData[param.Name!]);
        var deserialized = JsonSerializer.Deserialize<ResourceInvocationContext>(serialized, McpJsonSerializerOptions.DefaultOptions);
        Assert.Equal("test://resource/1", deserialized!.Uri);
    }

    [Fact]
    public async Task BindAsync_WithHttpContextAccessor_SetsHttpTransportAndHeaders()
    {
        var (binding, _) = CreateBinding();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Test"] = "abc";
        httpContext.Items[McpConstants.McpTransportName] = "http-sse";

        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var executionContext = ReadResourceExecutionContextHelper.CreateExecutionContext(httpContextAccessor: accessor);
        var triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        var resourceInvocationContext = (ResourceInvocationContext)triggerData.BindingData["mcpresourcecontext"];
        Assert.Equal("http-sse", resourceInvocationContext.Transport!.Name);

        var headers = Assert.IsType<Dictionary<string, string>>(resourceInvocationContext.Transport.Properties["headers"]);
        Assert.Equal("abc", headers["X-Test"]);
    }

    [Fact]
    public async Task BindAsync_WithSessionIdHeader_UsesHeaderSessionId()
    {
        var (binding, _) = CreateBinding();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[McpConstants.McpSessionIdHeaderName] = "header-session-123";
        httpContext.Items[McpConstants.McpTransportName] = "http";

        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var executionContext = ReadResourceExecutionContextHelper.CreateExecutionContext(
            sessionId: "default-session",
            httpContextAccessor: accessor);
        var triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        var resourceInvocationContext = (ResourceInvocationContext)triggerData.BindingData["mcpresourcecontext"];
        Assert.Equal("header-session-123", resourceInvocationContext.Transport!.SessionId);
        Assert.Equal("header-session-123", resourceInvocationContext.SessionId);
    }

    [Fact]
    public async Task BindAsync_WithoutHttpContextAccessor_UsesUnknownTransport()
    {
        var (binding, _) = CreateBinding();

        var executionContext = ReadResourceExecutionContextHelper.CreateExecutionContext();
        var triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        var resourceInvocationContext = (ResourceInvocationContext)triggerData.BindingData["mcpresourcecontext"];
        Assert.Equal("unknown", resourceInvocationContext.Transport!.Name);
    }

    [Fact]
    public async Task BindAsync_InvalidValue_Throws()
    {
        var (binding, _) = CreateBinding();
        await Assert.ThrowsAsync<InvalidOperationException>(() => binding.BindAsync("wrong", CreateValueBindingContext()));
    }

    [Fact]
    public async Task BindAsync_SetsClientInfo()
    {
        var (binding, _) = CreateBinding();
        var clientInfo = new Implementation { Name = "TestClient", Version = "2.0" };

        var executionContext = ReadResourceExecutionContextHelper.CreateExecutionContext(clientInfo: clientInfo);
        var triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        var resourceInvocationContext = (ResourceInvocationContext)triggerData.BindingData["mcpresourcecontext"];
        Assert.NotNull(resourceInvocationContext.ClientInfo);
        Assert.Equal("TestClient", resourceInvocationContext.ClientInfo.Name);
        Assert.Equal("2.0", resourceInvocationContext.ClientInfo.Version);
    }

    [Fact]
    public async Task BindAsync_SetsResourceUri()
    {
        var (binding, _) = CreateBinding(uri: "test://resource/custom");

        var executionContext = ReadResourceExecutionContextHelper.CreateExecutionContext(uri: "test://resource/custom");
        var triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        Assert.Equal("test://resource/custom", triggerData.BindingData["mcpresourceuri"]);
    }

    [Fact]
    public async Task CreateListenerAsync_CreatesResourceListener()
    {
        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        var resourceRegistry = new Mock<IResourceRegistry>();

        var listenerFactoryContext = new ListenerFactoryContext(
            new FunctionDescriptor { ShortName = "MyFunction" },
            executorMock.Object,
            CancellationToken.None);

        var attribute = new McpResourceTriggerAttribute("test://resource/1", "TestResource")
        {
            Description = "Test resource",
            MimeType = "text/plain",
            Size = 1024
        };

        var method = typeof(McpResourceTriggerBindingTests).GetMethod(nameof(DummyMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var parameter = method.GetParameters()[0];

        var binding = new McpResourceTriggerBinding(parameter, resourceRegistry.Object, attribute, NullLoggerFactory.Instance);

        var listener = await binding.CreateListenerAsync(listenerFactoryContext);

        Assert.NotNull(listener);
        var resourceListener = Assert.IsType<McpResourceListener>(listener);
        Assert.Equal("MyFunction", resourceListener.FunctionName);
        Assert.Equal("test://resource/1", resourceListener.Uri);
        Assert.Equal("TestResource", resourceListener.Name);
        Assert.Equal("Test resource", resourceListener.Description);
        Assert.Equal("text/plain", resourceListener.MimeType);
        Assert.Equal(1024, resourceListener.Size);

        resourceRegistry.Verify(r => r.Register(It.IsAny<IMcpResource>()), Times.Once);
    }

    [Fact]
    public void ToParameterDescriptor_ReturnsCorrectDescriptor()
    {
        var (binding, _) = CreateBinding();

        var descriptor = binding.ToParameterDescriptor();

        Assert.Equal("McpResourceTrigger", descriptor.Name);
        Assert.Equal("McpResourceTrigger", descriptor.Type);
        Assert.NotNull(descriptor.DisplayHints);
    }

    [Fact]
    public void TriggerValueType_IsObject()
    {
        var (binding, _) = CreateBinding();

        Assert.Equal(typeof(object), binding.TriggerValueType);
    }

    [Fact]
    public async Task CreateListenerAsync_WithMetadata_IncludesMetadataInListener()
    {
        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        var resourceRegistry = new Mock<IResourceRegistry>();

        var listenerFactoryContext = new ListenerFactoryContext(
            new FunctionDescriptor { ShortName = "MyFunction" },
            executorMock.Object,
            CancellationToken.None);

        var metadata = """{"key1":"value1","key2":123,"key3":true}""";
        var attribute = new McpResourceTriggerAttribute("test://resource/1", "TestResource")
        {
            Metadata = metadata
        };

        var method = typeof(McpResourceTriggerBindingTests).GetMethod(nameof(DummyMethodWithMetadata), BindingFlags.NonPublic | BindingFlags.Static)!;
        var parameter = method.GetParameters()[0];

        var binding = new McpResourceTriggerBinding(parameter, resourceRegistry.Object, attribute, NullLoggerFactory.Instance);

        var listener = await binding.CreateListenerAsync(listenerFactoryContext);

        var resourceListener = Assert.IsType<McpResourceListener>(listener);
        Assert.NotNull(resourceListener.Metadata);
        Assert.Equal(3, resourceListener.Metadata.Count);

        var metadataDict = resourceListener.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Assert.Equal("value1", metadataDict["key1"]);
        Assert.Equal(123L, metadataDict["key2"]);
        Assert.Equal(true, metadataDict["key3"]);
    }

    [Fact]
    public async Task CreateListenerAsync_WithoutMetadata_HasEmptyMetadataCollection()
    {
        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        var resourceRegistry = new Mock<IResourceRegistry>();

        var listenerFactoryContext = new ListenerFactoryContext(
            new FunctionDescriptor { ShortName = "MyFunction" },
            executorMock.Object,
            CancellationToken.None);

        var attribute = new McpResourceTriggerAttribute("test://resource/1", "TestResource");

        var method = typeof(McpResourceTriggerBindingTests).GetMethod(nameof(DummyMethodWithoutMetadata), BindingFlags.NonPublic | BindingFlags.Static)!;
        var parameter = method.GetParameters()[0];

        var binding = new McpResourceTriggerBinding(parameter, resourceRegistry.Object, attribute, NullLoggerFactory.Instance);

        var listener = await binding.CreateListenerAsync(listenerFactoryContext);

        var resourceListener = Assert.IsType<McpResourceListener>(listener);
        Assert.NotNull(resourceListener.Metadata);
        Assert.Empty(resourceListener.Metadata);
    }

    [Fact]
    public async Task CreateListenerAsync_WithNullValueMetadata_IncludesNullValue()
    {
        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        var resourceRegistry = new Mock<IResourceRegistry>();

        var listenerFactoryContext = new ListenerFactoryContext(
            new FunctionDescriptor { ShortName = "MyFunction" },
            executorMock.Object,
            CancellationToken.None);

        var metadata = """{"nullKey":null}""";
        var attribute = new McpResourceTriggerAttribute("test://resource/1", "TestResource")
        {
            Metadata = metadata
        };

        var method = typeof(McpResourceTriggerBindingTests).GetMethod(nameof(DummyMethodWithoutMetadata), BindingFlags.NonPublic | BindingFlags.Static)!;
        var parameter = method.GetParameters()[0];

        var binding = new McpResourceTriggerBinding(parameter, resourceRegistry.Object, attribute, NullLoggerFactory.Instance);

        var listener = await binding.CreateListenerAsync(listenerFactoryContext);

        var resourceListener = Assert.IsType<McpResourceListener>(listener);
        Assert.NotNull(resourceListener.Metadata);
        Assert.Single(resourceListener.Metadata);

        var metadataDict = resourceListener.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Assert.True(metadataDict.ContainsKey("nullKey"));
        Assert.Null(metadataDict["nullKey"]);
    }
}
