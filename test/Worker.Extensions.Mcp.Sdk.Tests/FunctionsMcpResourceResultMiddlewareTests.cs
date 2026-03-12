// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk;
using ModelContextProtocol.Protocol;
using Moq;

namespace Worker.Extensions.Mcp.Sdk.Tests;

public class FunctionsMcpResourceResultMiddlewareTests
{
    private const string ResourceInvocationContextKey = "ResourceInvocationContext";
    private readonly Mock<IFunctionResultAccessor> _resultAccessorMock;
    private readonly FunctionsMcpResourceResultMiddleware _middleware;
    private object? _currentResult;

    public FunctionsMcpResourceResultMiddlewareTests()
    {
        _resultAccessorMock = new Mock<IFunctionResultAccessor>();
        _resultAccessorMock
            .Setup(a => a.GetResult(It.IsAny<FunctionContext>()))
            .Returns(() => _currentResult);
        _resultAccessorMock
            .Setup(a => a.SetResult(It.IsAny<FunctionContext>(), It.IsAny<object?>()))
            .Callback<FunctionContext, object?>((_, value) => _currentResult = value);

        _middleware = new FunctionsMcpResourceResultMiddleware(_resultAccessorMock.Object);
    }

    [Fact]
    public async Task Invoke_WithStringResult_LeavesPrimitiveUntouched()
    {
        var context = CreateMcpResourceFunctionContext();

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, "plain text");
            return Task.CompletedTask;
        });

        Assert.Equal("plain text", _currentResult);
    }

    [Fact]
    public async Task Invoke_WithTextResourceContents_ThrowsUnsupportedType()
    {
        var context = CreateMcpResourceFunctionContext();
        var resource = new TextResourceContents
        {
            Uri = "ui://widget/welcome.html",
            MimeType = "text/html",
            Text = "<html>Hello</html>"
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, resource);
            return Task.CompletedTask;
        }));

        Assert.Contains("TextResourceContents", exception.Message);
        Assert.Contains("FileResourceContents", exception.Message);
    }

    [Fact]
    public async Task Invoke_WithFileResourceContents_WrapsAsResourceEnvelope()
    {
        var context = CreateMcpResourceFunctionContext();
        var resource = new FileResourceContents
        {
            Uri = "ui://widget/welcome.html",
            MimeType = "text/html+skybridge",
            Path = "c:/app/assets/welcome.html"
        };
        resource.Meta["openai/widgetPrefersBorder"] = true;

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, resource);
            return Task.CompletedTask;
        });

        var result = Assert.IsType<string>(_currentResult);
        using var envelope = JsonDocument.Parse(result);
        var content = envelope.RootElement.GetProperty("Content").GetString();
        Assert.NotNull(content);

        using var serializedResource = JsonDocument.Parse(content);
        Assert.Equal("ui://widget/welcome.html", serializedResource.RootElement.GetProperty("uri").GetString());
        Assert.Equal("text/html+skybridge", serializedResource.RootElement.GetProperty("mimeType").GetString());
        Assert.Equal("c:/app/assets/welcome.html", serializedResource.RootElement.GetProperty("path").GetString());
        Assert.True(serializedResource.RootElement.GetProperty("meta").GetProperty("openai/widgetPrefersBorder").GetBoolean());
    }

    private static FunctionContext CreateMcpResourceFunctionContext()
    {
        var items = new Dictionary<object, object>
        {
            { ResourceInvocationContextKey, new ResourceInvocationContext("file://resource") }
        };

        var functionDefinitionMock = new Mock<FunctionDefinition>();
        functionDefinitionMock.SetupGet(d => d.OutputBindings).Returns(ImmutableDictionary<string, BindingMetadata>.Empty);

        var contextMock = new Mock<FunctionContext>();
        contextMock.SetupGet(c => c.Items).Returns(items);
        contextMock.SetupGet(c => c.FunctionDefinition).Returns(functionDefinitionMock.Object);

        return contextMock.Object;
    }

    private void SetInvocationResult(FunctionContext context, object? value)
    {
        _currentResult = value;
    }
}