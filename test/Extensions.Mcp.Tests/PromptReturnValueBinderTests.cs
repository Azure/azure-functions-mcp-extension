// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using ModelContextProtocol;
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
    public async Task SetValueAsync_WithGetPromptResultEnvelope_DeserializesInner()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        var inner = new GetPromptResult
        {
            Description = "Test prompt",
            Messages =
            [
                new PromptMessage { Role = Role.User, Content = new TextContentBlock { Text = "User message" } },
                new PromptMessage { Role = Role.Assistant, Content = new TextContentBlock { Text = "Assistant message" } }
            ]
        };

        await binder.SetValueAsync(SerializeEnvelope(McpConstants.PromptResultContentTypes.GetPromptResult, inner), CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Equal("Test prompt", result.Description);
        Assert.Equal(2, result.Messages.Count);
        Assert.Equal(Role.User, result.Messages[0].Role);
        Assert.Equal(Role.Assistant, result.Messages[1].Role);
    }

    [Fact]
    public async Task SetValueAsync_WithEmptyGetPromptResultEnvelope_PreservesEmpty()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        var inner = new GetPromptResult { Messages = [] };

        await binder.SetValueAsync(SerializeEnvelope(McpConstants.PromptResultContentTypes.GetPromptResult, inner), CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Empty(result.Messages);
        Assert.Null(result.Description);
    }

    [Fact]
    public async Task SetValueAsync_WithDescriptionOnlyEnvelope_DeserializesInner()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        var inner = new GetPromptResult { Description = "some description" };

        await binder.SetValueAsync(SerializeEnvelope(McpConstants.PromptResultContentTypes.GetPromptResult, inner), CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Equal("some description", result.Description);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public async Task SetValueAsync_WithPromptMessagesEnvelope_WrapsInGetPromptResult()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        var messages = new List<PromptMessage>
        {
            new() { Role = Role.User, Content = new TextContentBlock { Text = "Hello" } }
        };

        await binder.SetValueAsync(SerializeEnvelope(McpConstants.PromptResultContentTypes.PromptMessages, messages), CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        Assert.Equal(Role.User, result.Messages[0].Role);
        Assert.Equal("Hello", Assert.IsType<TextContentBlock>(result.Messages[0].Content).Text);
    }

    [Fact]
    public async Task SetValueAsync_WithUnknownEnvelopeType_Throws()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        var envelope = new McpPromptResult
        {
            Type = "not_a_known_type",
            Content = "{}"
        };
        var json = JsonSerializer.Serialize(envelope, McpJsonSerializerOptions.DefaultOptions);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => binder.SetValueAsync(json, CancellationToken.None));

        Assert.Contains("Unknown McpPromptResult type", exception.Message);
    }

    [Fact]
    public async Task SetValueAsync_WithMissingContent_Throws()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        var envelope = new McpPromptResult
        {
            Type = McpConstants.PromptResultContentTypes.GetPromptResult,
            Content = null
        };
        var json = JsonSerializer.Serialize(envelope, McpJsonSerializerOptions.DefaultOptions);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => binder.SetValueAsync(json, CancellationToken.None));

        Assert.Contains("Content was null", exception.Message);
    }

    [Fact]
    public async Task SetValueAsync_WithNonStringValue_ThrowsArgumentException()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        await Assert.ThrowsAsync<ArgumentException>(
            () => binder.SetValueAsync(42, CancellationToken.None));
    }

    [Fact]
    public async Task SetValueAsync_WithInvalidJson_ThrowsInvalidOperationException()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new PromptReturnValueBinder(executionContext);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => binder.SetValueAsync("not a json { string", CancellationToken.None));
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

    private static string SerializeEnvelope<T>(string type, T inner)
    {
        var envelope = new McpPromptResult
        {
            Type = type,
            Content = JsonSerializer.Serialize(inner, McpJsonUtilities.DefaultOptions)
        };
        return JsonSerializer.Serialize(envelope, McpJsonSerializerOptions.DefaultOptions);
    }
}
