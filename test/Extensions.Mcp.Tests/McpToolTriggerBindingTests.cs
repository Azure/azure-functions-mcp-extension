// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpToolTriggerBindingTests
{
    private static (McpToolTriggerBinding binding, ParameterInfo parameter) CreateBinding(ParameterInfo? parameter = null)
    {
        if (parameter is null)
        {
            var method = typeof(McpToolTriggerBindingTests).GetMethod(nameof(DummyMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
            parameter = method.GetParameters()[0];
        }

        var toolRegistry = new Mock<IToolRegistry>();
        var attribute = new McpToolTriggerAttribute("MyTool", "desc");

        var binding = new McpToolTriggerBinding(parameter, toolRegistry.Object, attribute);

        return (binding, parameter);
    }

    private static void DummyMethod([McpToolTrigger("MyTool", "desc")] ToolInvocationContext ctx) { }

    private static void DummyStringMethod([McpToolTrigger("MyTool", "desc")] string ctx) { }

    private static ValueBindingContext CreateValueBindingContext()
    {
        var functionContext = new FunctionBindingContext(Guid.NewGuid(), CancellationToken.None);
        return new ValueBindingContext(functionContext, CancellationToken.None);
    }

    [Fact]
    public async Task BindAsync_BasicBinding_PopulatesBindingData()
    {
        var (binding, param) = CreateBinding();
        CallToolExecutionContext executionContext = CallToolExecutionContextHelper.CreateExecutionContext();
        var triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        Assert.True(triggerData.BindingData.ContainsKey("mcptoolcontext"));
        Assert.True(triggerData.BindingData.ContainsKey(param.Name!));
        Assert.True(triggerData.BindingData.ContainsKey("mcptoolargs"));
        Assert.True(triggerData.BindingData.ContainsKey("mcpsessionid"));

        var ctx = Assert.IsType<ToolInvocationContext>(triggerData.BindingData["mcptoolcontext"]);
        Assert.Equal("MyTool", ctx.Name);
        Assert.Equal("session-123", ctx.SessionId);
    }

    [Fact]
    public async Task BindAsync_StringParameter_SerializesContext()
    {
        var method = typeof(McpToolTriggerBindingTests).GetMethod(nameof(DummyStringMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var param = method.GetParameters()[0];

        var (binding, _) = CreateBinding(param);
        CallToolExecutionContext executionContext = CallToolExecutionContextHelper.CreateExecutionContext();
        ITriggerData triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        var serialized = Assert.IsType<string>(triggerData.BindingData[param.Name!]);
        var deserialized = JsonSerializer.Deserialize<ToolInvocationContext>(serialized, McpJsonSerializerOptions.DefaultOptions);
        Assert.Equal("MyTool", deserialized!.Name);
    }

    [Fact]
    public async Task BindAsync_WithHttpContextAccessor_SetsHttpTransportAndHeaders()
    {
        var (binding, _) = CreateBinding();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Test"] = "abc";
        httpContext.Items[McpConstants.McpTransportName] = "http-sse";

        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        CallToolExecutionContext executionContext = CallToolExecutionContextHelper.CreateExecutionContext(httpContextAccessor: accessor);
        ITriggerData triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        var toolInvocationContext = (ToolInvocationContext)triggerData.BindingData["mcptoolcontext"];
        Assert.Equal("http-sse", toolInvocationContext.Transport!.Name);

        var headers = Assert.IsType<Dictionary<string, string>>(toolInvocationContext.Transport.Properties["headers"]);
        Assert.Equal("abc", headers["X-Test"]);
    }

    [Fact]
    public async Task BindAsync_InvalidValue_Throws()
    {
        var (binding, _) = CreateBinding();
        await Assert.ThrowsAsync<InvalidOperationException>(() => binding.BindAsync("wrong", CreateValueBindingContext()));
    }

    [Fact]
    public void BindingDataContract_ContainsExpectedKeys()
    {
        var (binding, param) = CreateBinding();
        Assert.Contains("mcptoolcontext", binding.BindingDataContract.Keys);
        Assert.Contains("mcptoolargs", binding.BindingDataContract.Keys);
        Assert.Contains("mcpsessionid", binding.BindingDataContract.Keys);
        Assert.Contains(param.Name!, binding.BindingDataContract.Keys);
        Assert.Contains("$return", binding.BindingDataContract.Keys);
    }

    [Fact]
    public async Task BindAsync_BindingData_IsCaseInsensitive()
    {
        var (binding, param) = CreateBinding();
        CallToolExecutionContext executionContext = CallToolExecutionContextHelper.CreateExecutionContext();
        var triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        // Access using different casings
        var ctxLower = triggerData.BindingData["mcptoolcontext"];
        var ctxMixed = triggerData.BindingData["McpToolContext"];
        var ctxUpper = triggerData.BindingData["MCPTOOLCONTEXT"];

        Assert.Same(ctxLower, ctxMixed);
        Assert.Same(ctxLower, ctxUpper);

        // Parameter name
        var paramValueOriginal = triggerData.BindingData[param.Name!];
        var paramValueUpper = triggerData.BindingData[param.Name!.ToUpperInvariant()];
        Assert.Same(paramValueOriginal, paramValueUpper);

        // mcptoolargs and mcpsessionid
        Assert.NotNull(triggerData.BindingData["McpToolArgs"]);
        Assert.NotNull(triggerData.BindingData["McpSessionId"]);
    }

    [Fact]
    public void BindingDataContract_IsCaseInsensitive()
    {
        var (binding, param) = CreateBinding();

        // Verify case-insensitive ContainsKey behavior
        Assert.True(binding.BindingDataContract.ContainsKey("McpToolContext"));
        Assert.True(binding.BindingDataContract.ContainsKey("McpToolArgs"));
        Assert.True(binding.BindingDataContract.ContainsKey("McpSessionId"));
        Assert.True(binding.BindingDataContract.ContainsKey(param.Name!.ToUpperInvariant()));
        Assert.True(binding.BindingDataContract.ContainsKey("$RETURN")); // different case
    }
}
