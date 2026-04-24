// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using Moq;

namespace Worker.Extensions.Mcp.Sdk.Tests;

public class FunctionsMcpPromptResultMiddlewareTests
{
    private readonly Mock<IFunctionResultAccessor> _resultAccessorMock;
    private readonly FunctionsMcpPromptResultMiddleware _middleware;
    private object? _currentResult;

    public FunctionsMcpPromptResultMiddlewareTests()
    {
        _resultAccessorMock = new Mock<IFunctionResultAccessor>();
        _resultAccessorMock
            .Setup(a => a.GetResult(It.IsAny<FunctionContext>()))
            .Returns(() => _currentResult);
        _resultAccessorMock
            .Setup(a => a.SetResult(It.IsAny<FunctionContext>(), It.IsAny<object?>()))
            .Callback<FunctionContext, object?>((ctx, value) => _currentResult = value);
        _middleware = new FunctionsMcpPromptResultMiddleware(_resultAccessorMock.Object);
    }

    [Fact]
    public async Task Invoke_WithNonMcpPromptContext_DoesNotModifyResult()
    {
        var context = CreateFunctionContextWithoutPromptInvocationContext();
        var originalValue = "original value";
        SetInvocationResult(context, originalValue);
        var nextCalled = false;

        Task Next(FunctionContext _)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        await _middleware.Invoke(context, Next);

        Assert.True(nextCalled);
        Assert.Equal(originalValue, _currentResult);
    }

    [Fact]
    public async Task Invoke_WithNullResult_DoesNotModifyResult()
    {
        var context = CreateMcpPromptFunctionContext();
        SetInvocationResult(context, null);

        await _middleware.Invoke(context, _ => Task.CompletedTask);

        Assert.Null(_currentResult);
    }

    [Fact]
    public async Task Invoke_WithStringResult_WrapsInGetPromptResultWithTextMessage()
    {
        var context = CreateMcpPromptFunctionContext();
        var expectedText = "Please summarize the following text...";

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, expectedText);
            return Task.CompletedTask;
        });

        var promptResult = DeserializePromptResult();
        Assert.Single(promptResult.Messages);

        var message = promptResult.Messages[0];
        Assert.Equal(Role.User, message.Role);
        var textBlock = Assert.IsType<TextContentBlock>(message.Content);
        Assert.Equal(expectedText, textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithGetPromptResult_SerializesDirectly()
    {
        var context = CreateMcpPromptFunctionContext();
        var getPromptResult = new GetPromptResult
        {
            Description = "Code review for Python",
            Messages =
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock { Text = "Review this code..." }
                }
            ]
        };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, getPromptResult);
            return Task.CompletedTask;
        });

        var promptResult = DeserializePromptResult();
        Assert.Equal("Code review for Python", promptResult.Description);
        Assert.Single(promptResult.Messages);

        var textBlock = Assert.IsType<TextContentBlock>(promptResult.Messages[0].Content);
        Assert.Equal("Review this code...", textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithSinglePromptMessage_WrapsInGetPromptResult()
    {
        var context = CreateMcpPromptFunctionContext();
        var promptMessage = new PromptMessage
        {
            Role = Role.Assistant,
            Content = new TextContentBlock { Text = "I'll review your code now." }
        };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, promptMessage);
            return Task.CompletedTask;
        });

        var promptResult = DeserializePromptResult();
        Assert.Single(promptResult.Messages);
        Assert.Equal(Role.Assistant, promptResult.Messages[0].Role);

        var textBlock = Assert.IsType<TextContentBlock>(promptResult.Messages[0].Content);
        Assert.Equal("I'll review your code now.", textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithPromptMessageList_WrapsInGetPromptResult()
    {
        var context = CreateMcpPromptFunctionContext();
        var messages = new List<PromptMessage>
        {
            new() { Role = Role.User, Content = new TextContentBlock { Text = "Review this code" } },
            new() { Role = Role.Assistant, Content = new TextContentBlock { Text = "Here is my review..." } }
        };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, messages);
            return Task.CompletedTask;
        });

        var promptResult = DeserializePromptResult();
        Assert.Equal(2, promptResult.Messages.Count);
        Assert.Equal(Role.User, promptResult.Messages[0].Role);
        Assert.Equal(Role.Assistant, promptResult.Messages[1].Role);
    }

    [Fact]
    public async Task Invoke_WithObjectResult_SerializesAsJsonTextInPromptResult()
    {
        var context = CreateMcpPromptFunctionContext();
        var poco = new { name = "Alice", score = 95 };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, poco);
            return Task.CompletedTask;
        });

        var promptResult = DeserializePromptResult();
        Assert.Single(promptResult.Messages);

        var textBlock = Assert.IsType<TextContentBlock>(promptResult.Messages[0].Content);
        Assert.Contains("Alice", textBlock.Text);
        Assert.Contains("95", textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithGetPromptResultWithMultipleMessages_PreservesAll()
    {
        var context = CreateMcpPromptFunctionContext();
        var getPromptResult = new GetPromptResult
        {
            Messages =
            [
                new PromptMessage { Role = Role.User, Content = new TextContentBlock { Text = "What does this code do?" } },
                new PromptMessage { Role = Role.Assistant, Content = new TextContentBlock { Text = "This code implements a sorting algorithm..." } },
                new PromptMessage { Role = Role.User, Content = new TextContentBlock { Text = "Can you optimize it?" } }
            ]
        };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, getPromptResult);
            return Task.CompletedTask;
        });

        var promptResult = DeserializePromptResult();
        Assert.Equal(3, promptResult.Messages.Count);
    }

    [Fact]
    public async Task Invoke_WithOutputBindings_DoesNotModifyResult()
    {
        var outputBindings = new Dictionary<string, BindingMetadata>
        {
            { "$return", CreateOutputBindingMetadata() }
        }.ToImmutableDictionary();

        var context = CreateMcpPromptFunctionContextWithOutputBindings(outputBindings);

        var getPromptResult = new GetPromptResult
        {
            Messages =
            [
                new PromptMessage { Role = Role.User, Content = new TextContentBlock { Text = "test" } }
            ]
        };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, getPromptResult);
            return Task.CompletedTask;
        });

        Assert.IsType<GetPromptResult>(_currentResult);
    }

    [Fact]
    public async Task Invoke_WithToolContext_DoesNotProcessAsPrompt()
    {
        var items = new Dictionary<object, object>
        {
            { Constants.ToolInvocationContextKey, new ToolInvocationContext { Name = "TestTool" } }
        };

        var functionDefinitionMock = new Mock<FunctionDefinition>();
        functionDefinitionMock.SetupGet(d => d.OutputBindings).Returns(ImmutableDictionary<string, BindingMetadata>.Empty);

        var contextMock = new Mock<FunctionContext>();
        contextMock.SetupGet(c => c.Items).Returns(items);
        contextMock.SetupGet(c => c.FunctionDefinition).Returns(functionDefinitionMock.Object);

        var originalValue = "original";
        SetInvocationResult(contextMock.Object, originalValue);

        await _middleware.Invoke(contextMock.Object, _ => Task.CompletedTask);

        Assert.Equal(originalValue, _currentResult);
    }

    private GetPromptResult DeserializePromptResult()
    {
        var json = Assert.IsType<string>(_currentResult);
        var promptResult = JsonSerializer.Deserialize<GetPromptResult>(json, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(promptResult);
        return promptResult!;
    }

    private static FunctionContext CreateMcpPromptFunctionContext()
    {
        var items = new Dictionary<object, object>
        {
            { Constants.PromptInvocationContextKey, new PromptInvocationContext { Name = "TestPrompt" } }
        };

        var functionDefinitionMock = new Mock<FunctionDefinition>();
        functionDefinitionMock.SetupGet(d => d.OutputBindings).Returns(ImmutableDictionary<string, BindingMetadata>.Empty);

        var contextMock = new Mock<FunctionContext>();
        contextMock.SetupGet(c => c.Items).Returns(items);
        contextMock.SetupGet(c => c.FunctionDefinition).Returns(functionDefinitionMock.Object);

        return contextMock.Object;
    }

    private static FunctionContext CreateFunctionContextWithoutPromptInvocationContext()
    {
        var items = new Dictionary<object, object>();

        var functionDefinitionMock = new Mock<FunctionDefinition>();
        functionDefinitionMock.SetupGet(d => d.OutputBindings).Returns(ImmutableDictionary<string, BindingMetadata>.Empty);

        var contextMock = new Mock<FunctionContext>();
        contextMock.SetupGet(c => c.Items).Returns(items);
        contextMock.SetupGet(c => c.FunctionDefinition).Returns(functionDefinitionMock.Object);

        return contextMock.Object;
    }

    private static FunctionContext CreateMcpPromptFunctionContextWithOutputBindings(
        IImmutableDictionary<string, BindingMetadata> outputBindings)
    {
        var items = new Dictionary<object, object>
        {
            { Constants.PromptInvocationContextKey, new PromptInvocationContext { Name = "TestPrompt" } }
        };

        var functionDefinitionMock = new Mock<FunctionDefinition>();
        functionDefinitionMock.SetupGet(d => d.OutputBindings).Returns(outputBindings);

        var contextMock = new Mock<FunctionContext>();
        contextMock.SetupGet(c => c.Items).Returns(items);
        contextMock.SetupGet(c => c.FunctionDefinition).Returns(functionDefinitionMock.Object);

        return contextMock.Object;
    }

    private static BindingMetadata CreateOutputBindingMetadata()
    {
        var mock = new Mock<BindingMetadata>();
        mock.SetupGet(b => b.Direction).Returns(BindingDirection.Out);
        mock.SetupGet(b => b.Type).Returns("http");
        return mock.Object;
    }

    private void SetInvocationResult(FunctionContext context, object? value)
    {
        _currentResult = value;
    }
}
