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

    [Fact]
    public void HandleMcpToolResult_WithStructuredContent_PreservesStructuredContent()
    {
        // Arrange
        var originalStructuredContent = @"{""result"": ""success"", ""count"": 42}";
        var mcpToolResult = new McpToolResult
        {
            Type = "text",
            Content = JsonSerializer.Serialize(new TextContentBlock { Text = "Test result" }, McpJsonUtilities.DefaultOptions),
            StructuredContent = originalStructuredContent
        };

        // Act - simulate what the middleware does
        var resultJson = JsonSerializer.Serialize(mcpToolResult, McpJsonContext.Default.McpToolResult);
        var deserializedResult = JsonSerializer.Deserialize<McpToolResult>(resultJson, McpJsonContext.Default.McpToolResult);

        // Assert
        Assert.NotNull(deserializedResult);
        Assert.Equal("text", deserializedResult.Type);
        Assert.NotNull(deserializedResult.Content);
        Assert.Equal(originalStructuredContent, deserializedResult.StructuredContent);
    }

    [Fact]
    public void HandlePlainObject_WithoutStructuredContent_HasNullStructuredContent()
    {
        // Arrange
        var plainString = "Simple result";

        // Act - simulate middleware processing
        var type = Constants.TextContextResult;
        var content = JsonSerializer.Serialize(new TextContentBlock
        {
            Text = plainString
        }, McpJsonUtilities.DefaultOptions);

        var mcpToolResult = new McpToolResult
        {
            Type = type,
            Content = content,
            StructuredContent = null
        };

        // Assert
        Assert.Equal(Constants.TextContextResult, mcpToolResult.Type);
        Assert.NotNull(mcpToolResult.Content);
        Assert.Null(mcpToolResult.StructuredContent);
    }

    [Fact]
    public void HandleContentBlock_WithoutStructuredContent_HasNullStructuredContent()
    {
        // Arrange
        var textBlock = new TextContentBlock { Text = "Simple text block" };

        // Act - simulate middleware processing
        var type = textBlock.Type;
        var content = JsonSerializer.Serialize(textBlock, McpJsonUtilities.DefaultOptions);

        var mcpToolResult = new McpToolResult
        {
            Type = type,
            Content = content,
            StructuredContent = null
        };

        // Assert
        Assert.Equal("text", mcpToolResult.Type);
        Assert.NotNull(mcpToolResult.Content);
        Assert.Null(mcpToolResult.StructuredContent);
    }

    [Fact]
    public void McpToolResult_SerializationRoundTrip_WithStructuredContent()
    {
        // Arrange
        var originalResult = new McpToolResult
        {
            Type = "custom_result",
            Content = @"{""type"": ""text"", ""text"": ""Hello World""}",
            StructuredContent = @"{""status"": ""success"", ""metadata"": {""timestamp"": ""2024-01-01T00:00:00Z""}}"
        };

        // Act
        var json = JsonSerializer.Serialize(originalResult, McpJsonContext.Default.McpToolResult);
        var deserializedResult = JsonSerializer.Deserialize<McpToolResult>(json, McpJsonContext.Default.McpToolResult);

        // Assert
        Assert.NotNull(deserializedResult);
        Assert.Equal(originalResult.Type, deserializedResult.Type);
        Assert.Equal(originalResult.Content, deserializedResult.Content);
        Assert.Equal(originalResult.StructuredContent, deserializedResult.StructuredContent);
    }

    [Fact]
    public void McpToolResult_SerializationRoundTrip_WithoutStructuredContent()
    {
        // Arrange
        var originalResult = new McpToolResult
        {
            Type = "simple_result",
            Content = @"{""type"": ""text"", ""text"": ""Hello World""}",
            StructuredContent = null
        };

        // Act
        var json = JsonSerializer.Serialize(originalResult, McpJsonContext.Default.McpToolResult);
        var deserializedResult = JsonSerializer.Deserialize<McpToolResult>(json, McpJsonContext.Default.McpToolResult);

        // Assert
        Assert.NotNull(deserializedResult);
        Assert.Equal(originalResult.Type, deserializedResult.Type);
        Assert.Equal(originalResult.Content, deserializedResult.Content);
        Assert.Null(deserializedResult.StructuredContent);
    }

    private static FunctionContext CreateMcpFunctionContext()
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

        return contextMock.Object;
    }

    private static FunctionContext CreateFunctionContextWithoutToolInvocationContext()
    {
        var items = new Dictionary<object, object>();

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

    [Fact]
    public async Task Invoke_WithPocoWithMcpResultAttribute_CreatesStructuredContent()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var pocoResult = new TestPocoWithAttribute { Name = "Alice", Age = 30, Email = "alice@example.com" };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, pocoResult);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.TextContextResult, mcpToolResult.Type);

        // Verify structured content is populated
        Assert.NotNull(mcpToolResult.StructuredContent);
        Assert.Contains("\"name\":\"Alice\"", mcpToolResult.StructuredContent);
        Assert.Contains("\"age\":30", mcpToolResult.StructuredContent);
        Assert.Contains("\"email\":\"alice@example.com\"", mcpToolResult.StructuredContent);

        // Verify text content also contains the serialized JSON (backwards compatibility)
        var textBlock = JsonSerializer.Deserialize<TextContentBlock>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(textBlock);
        Assert.Contains("Alice", textBlock.Text);
        Assert.Contains("30", textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithPocoWithoutMcpResultAttribute_DoesNotCreateStructuredContent()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var pocoResult = new TestPoco { Name = "Bob", Age = 25, Email = "bob@example.com" };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, pocoResult);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.TextContextResult, mcpToolResult.Type);

        // Verify structured content is NOT created for POCO without attribute
        Assert.Null(mcpToolResult.StructuredContent);

        // Verify text content contains the serialized JSON
        var textBlock = JsonSerializer.Deserialize<TextContentBlock>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(textBlock);
        Assert.Contains("Bob", textBlock.Text);
        Assert.Contains("25", textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithArrayOfPocosWithMcpResultAttribute_CreatesStructuredContent()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var pocoArray = new[]
        {
            new TestPocoWithAttribute { Name = "Alice", Age = 30, Email = "alice@example.com" },
            new TestPocoWithAttribute { Name = "Bob", Age = 25, Email = "bob@example.com" }
        };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, pocoArray);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.TextContextResult, mcpToolResult.Type);

        // Verify structured content is created for array of POCOs with attribute
        Assert.NotNull(mcpToolResult.StructuredContent);
        Assert.Contains("\"name\":\"Alice\"", mcpToolResult.StructuredContent);
        Assert.Contains("\"name\":\"Bob\"", mcpToolResult.StructuredContent);
        Assert.Contains("\"age\":30", mcpToolResult.StructuredContent);
        Assert.Contains("\"age\":25", mcpToolResult.StructuredContent);

        // Verify text content also contains the serialized JSON
        var textBlock = JsonSerializer.Deserialize<TextContentBlock>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(textBlock);
        Assert.Contains("Alice", textBlock.Text);
        Assert.Contains("Bob", textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithArrayOfPocosWithoutMcpResultAttribute_DoesNotCreateStructuredContent()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var pocoArray = new[]
        {
            new TestPoco { Name = "Charlie", Age = 35, Email = "charlie@example.com" },
            new TestPoco { Name = "Diana", Age = 28, Email = "diana@example.com" }
        };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, pocoArray);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.TextContextResult, mcpToolResult.Type);

        // Verify structured content is NOT created for array of POCOs without attribute
        Assert.Null(mcpToolResult.StructuredContent);

        // Verify text content contains the serialized JSON
        var textBlock = JsonSerializer.Deserialize<TextContentBlock>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(textBlock);
        Assert.Contains("Charlie", textBlock.Text);
        Assert.Contains("Diana", textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithListOfPocosWithMcpResultAttribute_CreatesStructuredContent()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var pocoList = new List<TestPocoWithAttribute>
        {
            new TestPocoWithAttribute { Name = "Eve", Age = 32, Email = "eve@example.com" },
            new TestPocoWithAttribute { Name = "Frank", Age = 40, Email = "frank@example.com" }
        };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, pocoList);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.TextContextResult, mcpToolResult.Type);

        // Verify structured content is created for list of POCOs with attribute
        Assert.NotNull(mcpToolResult.StructuredContent);
        Assert.Contains("\"name\":\"Eve\"", mcpToolResult.StructuredContent);
        Assert.Contains("\"name\":\"Frank\"", mcpToolResult.StructuredContent);

        // Verify text content also contains the serialized JSON
        var textBlock = JsonSerializer.Deserialize<TextContentBlock>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(textBlock);
        Assert.Contains("Eve", textBlock.Text);
        Assert.Contains("Frank", textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithPrimitiveInt_DoesNotCreateStructuredContent()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var intResult = 42;

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, intResult);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.TextContextResult, mcpToolResult.Type);
        Assert.Null(mcpToolResult.StructuredContent); // Primitives don't get structured content

        var textBlock = JsonSerializer.Deserialize<TextContentBlock>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(textBlock);
        Assert.Equal("42", textBlock.Text);
    }

    [Fact]
    public async Task Invoke_WithDateTime_DoesNotCreateStructuredContent()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, dateTime);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Null(mcpToolResult.StructuredContent); // DateTime is excluded from POCO detection
    }

    [Fact]
    public async Task Invoke_WithGuid_DoesNotCreateStructuredContent()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var guid = Guid.NewGuid();

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, guid);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Null(mcpToolResult.StructuredContent); // Guid is excluded from POCO detection
    }

    [Fact]
    public async Task Invoke_WithCollection_DoesNotCreateStructuredContent()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var collection = new List<string> { "item1", "item2", "item3" };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, collection);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Null(mcpToolResult.StructuredContent); // Collections are excluded from POCO detection
    }

    [Fact]
    public async Task Invoke_WithCallToolResult_SerializesDirectly()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var metadata = new { Key = "value", Count = 42 };
        var metadataJson = JsonSerializer.Serialize(metadata);

        var callToolResult = new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = metadataJson },
                new ImageContentBlock { Data = "imagedata", MimeType = "image/jpeg" }
            },
            StructuredContent = System.Text.Json.Nodes.JsonNode.Parse(metadataJson)
        };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, callToolResult);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.CallToolResultType, mcpToolResult.Type);
        Assert.Null(mcpToolResult.StructuredContent); // Not preserved in McpToolResult wrapper

        // Verify the CallToolResult is serialized in content
        var deserializedCallToolResult = JsonSerializer.Deserialize<CallToolResult>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(deserializedCallToolResult);
        Assert.Equal(2, deserializedCallToolResult.Content.Count);
        Assert.NotNull(deserializedCallToolResult.StructuredContent);
    }

    [Fact]
    public async Task Invoke_WithCallToolResultWithTextContent_Succeeds()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var metadata = new { Status = "success", Code = 200 };
        var metadataJson = JsonSerializer.Serialize(metadata);

        var callToolResult = new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = metadataJson },
                new ImageContentBlock { Data = "imagedata", MimeType = "image/jpeg" }
            },
            StructuredContent = System.Text.Json.Nodes.JsonNode.Parse(metadataJson)
        };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, callToolResult);
            return Task.CompletedTask;
        });

        // Assert - Should not throw
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.CallToolResultType, mcpToolResult.Type);

        // Deserialize the CallToolResult from content
        var deserializedCallToolResult = JsonSerializer.Deserialize<CallToolResult>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(deserializedCallToolResult);
        Assert.NotNull(deserializedCallToolResult.StructuredContent);
    }

    [Fact]
    public async Task Invoke_WithCallToolResultWithoutStructuredContent_Succeeds()
    {
        // Arrange
        var context = CreateMcpFunctionContext();

        var callToolResult = new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = "Simple message" },
                new ImageContentBlock { Data = "imagedata", MimeType = "image/png" }
            }
            // No StructuredContent - this is fine
        };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, callToolResult);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.CallToolResultType, mcpToolResult.Type);
        Assert.Null(mcpToolResult.StructuredContent);

        // Deserialize the CallToolResult from content
        var deserializedCallToolResult = JsonSerializer.Deserialize<CallToolResult>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(deserializedCallToolResult);
        Assert.Equal(2, deserializedCallToolResult.Content.Count);
        Assert.Null(deserializedCallToolResult.StructuredContent);
    }

    [Fact]
    public async Task Invoke_WithCallToolResultEmptyContent_SerializesDirectly()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var callToolResult = new CallToolResult
        {
            Content = new List<ContentBlock>()
        };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, callToolResult);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.CallToolResultType, mcpToolResult.Type);

        // Deserialize the CallToolResult from content
        var deserializedCallToolResult = JsonSerializer.Deserialize<CallToolResult>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(deserializedCallToolResult);
        Assert.Empty(deserializedCallToolResult.Content);
    }

    [Fact]
    public async Task Invoke_WithCallToolResultSingleContent_UsesCallToolResultType()
    {
        // Arrange
        var context = CreateMcpFunctionContext();
        var callToolResult = new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = "Single content block" }
            }
        };

        // Act
        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, callToolResult);
            return Task.CompletedTask;
        });

        // Assert
        var result = _currentResult as string;
        Assert.NotNull(result);

        McpToolResult? mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Equal(Constants.CallToolResultType, mcpToolResult.Type); // Should use call_tool_result type

        // Deserialize the CallToolResult from content
        var deserializedCallToolResult = JsonSerializer.Deserialize<CallToolResult>(mcpToolResult.Content!, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(deserializedCallToolResult);
        Assert.Single(deserializedCallToolResult.Content);
    }

    private class TestPoco
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    [McpResult]
    private class TestPocoWithAttribute
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    #region ShouldCreateStructuredContent Test Types

    // Base class with [McpResult] attribute for inheritance tests
    [McpResult]
    private class BasePocoWithAttribute
    {
        public string BaseProperty { get; set; } = string.Empty;
    }

    // Derived class inherits from [McpResult] base
    private class DerivedPocoInheritingAttribute : BasePocoWithAttribute
    {
        public string DerivedProperty { get; set; } = string.Empty;
    }

    // Record class with [McpResult] attribute
    [McpResult]
    private record RecordWithAttribute(string Name, int Value);

    // Record class without attribute
    private record RecordWithoutAttribute(string Name, int Value);

    // Record with [McpResult] attribute for use as dictionary key
    // Note: System.Text.Json doesn't support complex types as dictionary keys by default
    [McpResult]
    private record RecordKeyWithAttribute(string Id);

    // Struct with [McpResult] attribute (though structs are less common)
    [McpResult]
    private struct StructWithAttribute
    {
        public string Data { get; set; }
    }

    // Struct without attribute
    private struct StructWithoutAttribute
    {
        public string Data { get; set; }
    }

    #endregion

    // ========================
    // Direct Attribution Tests
    // ========================

    [Fact]
    public async Task ShouldCreateStructuredContent_DirectAttribution_Class_ReturnsTrue()
    {
        var context = CreateMcpFunctionContext();
        var poco = new TestPocoWithAttribute { Name = "Test", Age = 25, Email = "test@example.com" };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, poco);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.NotNull(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_DirectAttribution_Record_ReturnsTrue()
    {
        var context = CreateMcpFunctionContext();
        var record = new RecordWithAttribute("RecordTest", 42);

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, record);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.NotNull(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_DirectAttribution_Struct_ReturnsTrue()
    {
        var context = CreateMcpFunctionContext();
        var structValue = new StructWithAttribute { Data = "StructData" };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, structValue);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.NotNull(mcpToolResult.StructuredContent);
    }

    // ========================
    // Inherited Attribution Tests
    // ========================

    [Fact]
    public async Task ShouldCreateStructuredContent_InheritedAttribution_DerivedClass_ReturnsTrue()
    {
        var context = CreateMcpFunctionContext();
        var derived = new DerivedPocoInheritingAttribute { BaseProperty = "Base", DerivedProperty = "Derived" };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, derived);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.NotNull(mcpToolResult.StructuredContent);
        Assert.Contains("derivedProperty", mcpToolResult.StructuredContent);
    }

    // ========================
    // Collection Element Attribution Tests
    // ========================

    [Fact]
    public async Task ShouldCreateStructuredContent_Array_ElementsWithAttribute_ReturnsTrue()
    {
        var context = CreateMcpFunctionContext();
        var array = new TestPocoWithAttribute[]
        {
                new() { Name = "Item1", Age = 1, Email = "item1@example.com" },
                new() { Name = "Item2", Age = 2, Email = "item2@example.com" }
        };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, array);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.NotNull(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_List_ElementsWithAttribute_ReturnsTrue()
    {
        var context = CreateMcpFunctionContext();
        var list = new List<TestPocoWithAttribute>
            {
                new() { Name = "ListItem1", Age = 10, Email = "list1@example.com" },
                new() { Name = "ListItem2", Age = 20, Email = "list2@example.com" }
            };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, list);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.NotNull(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_IEnumerable_ElementsWithAttribute_ReturnsTrue()
    {
        var context = CreateMcpFunctionContext();
        IEnumerable<TestPocoWithAttribute> enumerable = new[]
        {
                new TestPocoWithAttribute { Name = "EnumItem", Age = 30, Email = "enum@example.com" }
            };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, enumerable);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.NotNull(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_Dictionary_ValueTypeWithAttribute_ReturnsTrue()
    {
        var context = CreateMcpFunctionContext();
        var dictionary = new Dictionary<string, TestPocoWithAttribute>
        {
            ["key1"] = new() { Name = "DictValue1", Age = 100, Email = "dict1@example.com" },
            ["key2"] = new() { Name = "DictValue2", Age = 200, Email = "dict2@example.com" }
        };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, dictionary);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.NotNull(mcpToolResult.StructuredContent);
    }

    // ========================
    // Nested Collection Tests
    // ========================

    [Fact]
    public async Task ShouldCreateStructuredContent_NestedList_InnerElementsWithAttribute_ReturnsTrue()
    {
        var context = CreateMcpFunctionContext();
        var nestedList = new List<List<TestPocoWithAttribute>>
            {
                new()
                {
                    new() { Name = "Nested1", Age = 1, Email = "nested1@example.com" }
                },
                new()
                {
                    new() { Name = "Nested2", Age = 2, Email = "nested2@example.com" }
                }
            };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, nestedList);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.NotNull(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_ArrayOfArrays_InnerElementsWithAttribute_ReturnsTrue()
    {
        var context = CreateMcpFunctionContext();
        var jaggedArray = new TestPocoWithAttribute[][]
        {
                new[] { new TestPocoWithAttribute { Name = "Jagged1", Age = 1, Email = "j1@example.com" } },
                new[] { new TestPocoWithAttribute { Name = "Jagged2", Age = 2, Email = "j2@example.com" } }
        };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, jaggedArray);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.NotNull(mcpToolResult.StructuredContent);
    }

    // ========================
    // No Attribution Tests (should return false)
    // ========================

    [Fact]
    public async Task ShouldCreateStructuredContent_ClassWithoutAttribute_ReturnsFalse()
    {
        var context = CreateMcpFunctionContext();
        var poco = new TestPoco { Name = "NoAttr", Age = 50, Email = "noattr@example.com" };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, poco);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Null(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_RecordWithoutAttribute_ReturnsFalse()
    {
        var context = CreateMcpFunctionContext();
        var record = new RecordWithoutAttribute("NoAttrRecord", 99);

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, record);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Null(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_StructWithoutAttribute_ReturnsFalse()
    {
        var context = CreateMcpFunctionContext();
        var structValue = new StructWithoutAttribute { Data = "NoAttrStruct" };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, structValue);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Null(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_ArrayWithoutAttribute_ReturnsFalse()
    {
        var context = CreateMcpFunctionContext();
        var array = new TestPoco[]
        {
                new() { Name = "NoAttrArray1", Age = 1, Email = "arr1@example.com" }
        };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, array);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Null(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_ListWithoutAttribute_ReturnsFalse()
    {
        var context = CreateMcpFunctionContext();
        var list = new List<TestPoco>
            {
                new() { Name = "NoAttrList1", Age = 1, Email = "list1@example.com" }
            };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, list);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Null(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_DictionaryWithoutAttribute_ReturnsFalse()
    {
        var context = CreateMcpFunctionContext();
        var dictionary = new Dictionary<string, TestPoco>
        {
            ["key1"] = new() { Name = "NoAttrDict", Age = 1, Email = "dict@example.com" }
        };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, dictionary);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Null(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_PrimitiveString_ReturnsFalse()
    {
        var context = CreateMcpFunctionContext();

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, "primitive string");
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Null(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_PrimitiveInt_ReturnsFalse()
    {
        var context = CreateMcpFunctionContext();

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, 12345);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Null(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_PrimitiveBool_ReturnsFalse()
    {
        var context = CreateMcpFunctionContext();

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, true);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Null(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_ListOfStrings_ReturnsFalse()
    {
        var context = CreateMcpFunctionContext();
        var list = new List<string> { "a", "b", "c" };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, list);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Null(mcpToolResult.StructuredContent);
    }

    [Fact]
    public async Task ShouldCreateStructuredContent_DictionaryOfPrimitives_ReturnsFalse()
    {
        var context = CreateMcpFunctionContext();
        var dict = new Dictionary<string, int> { ["one"] = 1, ["two"] = 2 };

        await _middleware.Invoke(context, ctx =>
        {
            SetInvocationResult(ctx, dict);
            return Task.CompletedTask;
        });

        var result = _currentResult as string;
        Assert.NotNull(result);
        var mcpToolResult = JsonSerializer.Deserialize(result, McpJsonContext.Default.McpToolResult);
        Assert.NotNull(mcpToolResult);
        Assert.Null(mcpToolResult.StructuredContent);
    }
}
