// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using Moq;

namespace Worker.Extensions.Mcp.Sdk.Tests;

public class FunctionsMcpToolResultMiddlewareTests
{
    private readonly Mock<IFunctionResultAccessor> _resultAccessorMock;
    private readonly FunctionsMcpToolResultMiddleware _middleware;
    private object? _currentResult;

    public FunctionsMcpToolResultMiddlewareTests()
    {
        _resultAccessorMock = new Mock<IFunctionResultAccessor>();
        _resultAccessorMock
            .Setup(a => a.GetResult(It.IsAny<FunctionContext>()))
            .Returns(() => _currentResult);
        _resultAccessorMock
            .Setup(a => a.SetResult(It.IsAny<FunctionContext>(), It.IsAny<object?>()))
            .Callback<FunctionContext, object?>((ctx, value) => _currentResult = value);
        _middleware = new FunctionsMcpToolResultMiddleware(_resultAccessorMock.Object);
    }

    [Fact]
    public async Task Invoke_WithNonMcpToolContext_DoesNotModifyResult()
    {
        // Arrange
        var context = CreateFunctionContextWithoutToolInvocationContext();
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
        var context = CreateMcpFunctionContext();
        SetInvocationResult(context, null);

        // Act
        await _middleware.Invoke(context, _ => Task.CompletedTask);

        // Assert
        Assert.Null(_currentResult);
    }

    [Fact]
    public async Task Invoke_WithStringResult_CreatesTextContentBlock()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var expectedText = "Hello, MCP!";

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, expectedText);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.TextContextResult, mcpToolResult.Type);

        var textBlock = JsonSerializer.Deserialize<TextContentBlock>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(textBlock);
        Assert.Equal(expectedText, textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithObjectResult_SerializesAndCreatesTextContentBlock()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var complexObject = new { Name = "Test", Value = 42 };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, complexObject);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.TextContextResult, mcpToolResult.Type);

        var textBlock = JsonSerializer.Deserialize<TextContentBlock>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(textBlock);
        Assert.Contains("Test", textBlock.Text);
        Assert.Contains("42", textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithSingleContentBlock_SerializesCorrectly()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var contentBlock = new TextContentBlock { Text = "Content from block" };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, contentBlock);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal("text", mcpToolResult.Type);

        var deserializedBlock = JsonSerializer.Deserialize<TextContentBlock>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(deserializedBlock);
        Assert.Equal("Content from block", deserializedBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithImageContentBlock_SerializesCorrectly()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var contentBlock = new ImageContentBlock
        {
            Data = "base64data",
            MimeType = "image/png"
        };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, contentBlock);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal("image", mcpToolResult.Type);

        var deserializedBlock = JsonSerializer.Deserialize<ImageContentBlock>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(deserializedBlock);
        Assert.Equal("base64data", deserializedBlock.Data);
        Assert.Equal("image/png", deserializedBlock.MimeType);
    }

    [Fact]
    public async Task Invoke_WithMultipleContentBlocks_UsesMultiContentResultType()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var blocks = new List<ContentBlock>
        {
            new TextContentBlock { Text = "First block" },
            new TextContentBlock { Text = "Second block" }
        };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, blocks);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.MultiContentResult, mcpToolResult.Type);

        var deserializedBlocks = JsonSerializer.Deserialize<List<ContentBlock>>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(deserializedBlocks);
        Assert.Equal(2, deserializedBlocks.Count);
    }

    [Fact]
    public async Task Invoke_WithEmptyContentBlockList_SerializesCorrectly()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var blocks = new List<ContentBlock>();

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, blocks);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.MultiContentResult, mcpToolResult.Type);

        var deserializedBlocks = JsonSerializer.Deserialize<List<ContentBlock>>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(deserializedBlocks);
        Assert.Empty(deserializedBlocks);
    }

    [Fact]
    public async Task Invoke_WithMixedContentBlocks_SerializesAllTypes()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var blocks = new List<ContentBlock>
        {
            new TextContentBlock { Text = "Text content" },
            new ImageContentBlock { Data = "imagedata", MimeType = "image/jpeg" }
        };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, blocks);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.MultiContentResult, mcpToolResult.Type);
        Assert.NotNull(mcpToolResult.Content);
    }

    [Fact]
    public async Task Invoke_WithSpecialCharacters_HandlesJsonEscaping()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var textWithSpecialChars = "Hello \"World\"\nNew Line\tTab";

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, textWithSpecialChars);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);

        var textBlock = JsonSerializer.Deserialize<TextContentBlock>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(textBlock);
        Assert.Equal(textWithSpecialChars, textBlock.Text);
    }

    [Fact]
    public async Task Invoke_CallsNextDelegate()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var nextCalled = false;
        Task Next(FunctionContext ctx)
        {
            nextCalled = true;
            SetInvocationResult(ctx, "test");
            return Task.CompletedTask;
        }

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_ThrowsArgumentNullException_WhenContextIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _middleware.Invoke(null!, _ => Task.CompletedTask));
    }

    private static FunctionContext CreateMcpFunctionContext()
    {
        var items = new Dictionary<object, object>
        {
            { Constants.ToolInvocationContextKey, new ToolInvocationContext { Name = "TestTool" } }
        };

        var contextMock = new Mock<FunctionContext>();
        contextMock.SetupGet(c => c.Items).Returns(items);

        return contextMock.Object;
    }

    private static FunctionContext CreateFunctionContextWithoutToolInvocationContext()
    {
        var items = new Dictionary<object, object>();

        var contextMock = new Mock<FunctionContext>();
        contextMock.SetupGet(c => c.Items).Returns(items);

        return contextMock.Object;
    }

    private void SetInvocationResult(FunctionContext context, object? value)
    {
        _currentResult = value;
    }
}
