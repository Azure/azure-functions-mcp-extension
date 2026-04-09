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
        // Arrange
        var context = CreateFunctionContextWithoutPromptInvocationContext();
        var originalValue = "original value";
        SetInvocationResult(context, originalValue);
        var nextCalled = false;

        Task Next(FunctionContext _)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(originalValue, _currentResult);
    }

    [Fact]
    public async Task Invoke_WithNullResult_DoesNotModifyResult()
    {
        // Arrange
        var context = CreateMcpPromptFunctionContext();
        SetInvocationResult(context, null);

        // Act
        await _middleware.Invoke(context, _ => Task.CompletedTask);

        // Assert
        Assert.Null(_currentResult);
    }

    [Fact]
    public async Task Invoke_WithStringResult_WrapsInGetPromptResultWithTextMessage()
    {
        // Arrange
        var context = CreateMcpPromptFunctionContext();
        var expectedText = "Please summarize the following text...";

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, expectedText);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpPromptResult? mcpPromptResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpPromptResult);
        Assert.NotNull(mcpPromptResult);
        Assert.Equal(Constants.GetPromptResultType, mcpPromptResult.Type);

        var getPromptResult = JsonSerializer.Deserialize<GetPromptResult>(mcpPromptResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(getPromptResult);
        Assert.Single(getPromptResult.Messages);

        var message = getPromptResult.Messages[0];
        Assert.Equal(Role.User, message.Role);
        var textBlock = Assert.IsType<TextContentBlock>(message.Content);
        Assert.Equal(expectedText, textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithGetPromptResult_SerializesDirectly()
    {
        // Arrange
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

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, getPromptResult);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpPromptResult? mcpPromptResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpPromptResult);
        Assert.NotNull(mcpPromptResult);
        Assert.Equal(Constants.GetPromptResultType, mcpPromptResult.Type);

        var deserialized = JsonSerializer.Deserialize<GetPromptResult>(mcpPromptResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(deserialized);
        Assert.Equal("Code review for Python", deserialized.Description);
        Assert.Single(deserialized.Messages);

        var textBlock = Assert.IsType<TextContentBlock>(deserialized.Messages[0].Content);
        Assert.Equal("Review this code...", textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithSinglePromptMessage_WrapsInPromptMessages()
    {
        // Arrange
        var context = CreateMcpPromptFunctionContext();
        var promptMessage = new PromptMessage
        {
            Role = Role.Assistant,
            Content = new TextContentBlock { Text = "I'll review your code now." }
        };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, promptMessage);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpPromptResult? mcpPromptResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpPromptResult);
        Assert.NotNull(mcpPromptResult);
        Assert.Equal(Constants.PromptMessagesType, mcpPromptResult.Type);

        var messages = JsonSerializer.Deserialize<IList<PromptMessage>>(mcpPromptResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(messages);
        Assert.Single(messages);
        Assert.Equal(Role.Assistant, messages[0].Role);

        var textBlock = Assert.IsType<TextContentBlock>(messages[0].Content);
        Assert.Equal("I'll review your code now.", textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithPromptMessageList_SerializesAsList()
    {
        // Arrange
        var context = CreateMcpPromptFunctionContext();
        var messages = new List<PromptMessage>
        {
            new() { Role = Role.User, Content = new TextContentBlock { Text = "Review this code" } },
            new() { Role = Role.Assistant, Content = new TextContentBlock { Text = "Here is my review..." } }
        };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, messages);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpPromptResult? mcpPromptResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpPromptResult);
        Assert.NotNull(mcpPromptResult);
        Assert.Equal(Constants.PromptMessagesType, mcpPromptResult.Type);

        var deserialized = JsonSerializer.Deserialize<IList<PromptMessage>>(mcpPromptResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Count);
        Assert.Equal(Role.User, deserialized[0].Role);
        Assert.Equal(Role.Assistant, deserialized[1].Role);
    }

    [Fact]
    public async Task Invoke_WithObjectResult_WrapsAsJsonTextInPromptResult()
    {
        // Arrange
        var context = CreateMcpPromptFunctionContext();
        var poco = new { name = "Alice", score = 95 };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, poco);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpPromptResult? mcpPromptResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpPromptResult);
        Assert.NotNull(mcpPromptResult);
        Assert.Equal(Constants.GetPromptResultType, mcpPromptResult.Type);

        var getPromptResult = JsonSerializer.Deserialize<GetPromptResult>(mcpPromptResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(getPromptResult);
        Assert.Single(getPromptResult.Messages);

        var textBlock = Assert.IsType<TextContentBlock>(getPromptResult.Messages[0].Content);
        Assert.Contains("Alice", textBlock.Text);
        Assert.Contains("95", textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithGetPromptResultWithMultipleMessages_PreservesAll()
    {
        // Arrange
        var context = CreateMcpPromptFunctionContext();
        var getPromptResult = new GetPromptResult
        {
            Messages =
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock { Text = "What does this code do?" }
                },
                new PromptMessage
                {
                    Role = Role.Assistant,
                    Content = new TextContentBlock { Text = "This code implements a sorting algorithm..." }
                },
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock { Text = "Can you optimize it?" }
                }
            ]
        };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, getPromptResult);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpPromptResult? mcpPromptResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpPromptResult);
        Assert.NotNull(mcpPromptResult);

        var deserialized = JsonSerializer.Deserialize<GetPromptResult>(mcpPromptResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.Messages.Count);
    }

    [Fact]
    public async Task Invoke_WithOutputBindings_DoesNotModifyResult()
    {
        // Arrange
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

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, getPromptResult);
            return Task.CompletedTask;
        });

        // Assert - result should NOT be wrapped
        Assert.IsType<GetPromptResult>(_currentResult);
    }

    [Fact]
    public async Task Invoke_WithToolContext_DoesNotProcessAsPrompt()
    {
        // Arrange - create context with tool context, not prompt
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

        // Act
        await _middleware.Invoke(contextMock.Object, _ => Task.CompletedTask);

        // Assert - should not be modified (it's a tool invocation, not prompt)
        Assert.Equal(originalValue, _currentResult);
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
