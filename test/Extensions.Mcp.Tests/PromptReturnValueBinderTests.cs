// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class PromptReturnValueBinderTests
{
    [Fact]
    public async Task SetValueAsync_WithNull_SetsEmptyMessages()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        await binder.SetValueAsync(null!, CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public async Task SetValueAsync_WithPlainString_WrapsAsUserMessage()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        await binder.SetValueAsync("Please review this code", CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        Assert.Equal(Role.User, result.Messages[0].Role);
        var content = Assert.IsType<TextContentBlock>(result.Messages[0].Content);
        Assert.Equal("Please review this code", content.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithGetPromptResultJson_DeserializesDirectly()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        var getPromptResult = new GetPromptResult
        {
            Description = "Test prompt",
            Messages =
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock { Text = "User message" }
                },
                new PromptMessage
                {
                    Role = Role.Assistant,
                    Content = new TextContentBlock { Text = "Assistant message" }
                }
            ]
        };

        var json = JsonSerializer.Serialize(getPromptResult, ModelContextProtocol.McpJsonUtilities.DefaultOptions);
        await binder.SetValueAsync(json, CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Equal(2, result.Messages.Count);
        Assert.Equal(Role.User, result.Messages[0].Role);
        Assert.Equal(Role.Assistant, result.Messages[1].Role);
    }

    [Fact]
    public async Task SetValueAsync_WithInvalidJson_WrapsAsUserMessage()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        await binder.SetValueAsync("not a json { string", CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        Assert.Equal(Role.User, result.Messages[0].Role);
        var content = Assert.IsType<TextContentBlock>(result.Messages[0].Content);
        Assert.Equal("not a json { string", content.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithJsonWithoutMessagesField_DeserializesAsEmptyPromptResult()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        // JSON that can deserialize to GetPromptResult but has no messages field.
        // GetPromptResult initializes Messages to empty list by default,
        // so this will successfully deserialize with empty messages.
        var json = """{"description": "some description"}""";
        await binder.SetValueAsync(json, CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.NotNull(result.Messages);
        Assert.Empty(result.Messages);
        Assert.Equal("some description", result.Description);
    }

    [Fact]
    public async Task SetValueAsync_WithUnsupportedType_ThrowsInvalidOperationException()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => binder.SetValueAsync(42, CancellationToken.None));

        Assert.Contains("Unsupported return type", exception.Message);
        Assert.Contains("Int32", exception.Message);
    }

    [Fact]
    public async Task GetValueAsync_ThrowsNotSupportedException()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        await Assert.ThrowsAsync<NotSupportedException>(() => binder.GetValueAsync());
    }

    [Fact]
    public void ToInvokeString_ReturnsEmptyString()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        Assert.Equal(string.Empty, binder.ToInvokeString());
    }

    [Fact]
    public async Task SetValueAsync_WithEmptyString_WrapsAsUserMessage()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        await binder.SetValueAsync("", CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        var content = Assert.IsType<TextContentBlock>(result.Messages[0].Content);
        Assert.Equal("", content.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithMcpPromptResultWrapper_GetPromptResultType_DeserializesCorrectly()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        // Simulate what the SDK middleware produces for a GetPromptResult return type
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

        var mcpPromptResult = new McpPromptResult
        {
            Type = "get_prompt_result",
            Content = JsonSerializer.Serialize(getPromptResult, ModelContextProtocol.McpJsonUtilities.DefaultOptions)
        };

        var json = JsonSerializer.Serialize(mcpPromptResult, Serialization.McpJsonSerializerOptions.DefaultOptions);
        await binder.SetValueAsync(json, CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Equal("Code review for Python", result.Description);
        Assert.Single(result.Messages);
        Assert.Equal(Role.User, result.Messages[0].Role);
        var textBlock = Assert.IsType<TextContentBlock>(result.Messages[0].Content);
        Assert.Equal("Review this code...", textBlock.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithMcpPromptResultWrapper_PromptMessagesType_DeserializesCorrectly()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        // Simulate what the SDK middleware produces for a PromptMessage list return type
        var messages = new List<PromptMessage>
        {
            new() { Role = Role.User, Content = new TextContentBlock { Text = "Question" } },
            new() { Role = Role.Assistant, Content = new TextContentBlock { Text = "Answer" } }
        };

        var mcpPromptResult = new McpPromptResult
        {
            Type = "prompt_messages",
            Content = JsonSerializer.Serialize(messages, ModelContextProtocol.McpJsonUtilities.DefaultOptions)
        };

        var json = JsonSerializer.Serialize(mcpPromptResult, Serialization.McpJsonSerializerOptions.DefaultOptions);
        await binder.SetValueAsync(json, CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Equal(2, result.Messages.Count);
        Assert.Equal(Role.User, result.Messages[0].Role);
        Assert.Equal(Role.Assistant, result.Messages[1].Role);
    }

    [Fact]
    public async Task SetValueAsync_WithMcpPromptResultWrapper_UnknownType_ThrowsInvalidOperationException()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        var mcpPromptResult = new McpPromptResult
        {
            Type = "unknown_type",
            Content = "{}"
        };

        var json = JsonSerializer.Serialize(mcpPromptResult, Serialization.McpJsonSerializerOptions.DefaultOptions);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => binder.SetValueAsync(json, CancellationToken.None));

        Assert.Contains("unknown_type", exception.Message);
    }
}
