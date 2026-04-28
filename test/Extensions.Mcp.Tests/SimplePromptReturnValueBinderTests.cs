// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class SimplePromptReturnValueBinderTests
{
    [Fact]
    public async Task SetValueAsync_WithNull_SetsEmptyMessages()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new SimplePromptReturnValueBinder(executionContext);

        await binder.SetValueAsync(null!, CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public async Task SetValueAsync_WithPlainString_WrapsAsUserTextMessage()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new SimplePromptReturnValueBinder(executionContext);

        await binder.SetValueAsync("Please review this code", CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        Assert.Equal(Role.User, result.Messages[0].Role);
        var content = Assert.IsType<TextContentBlock>(result.Messages[0].Content);
        Assert.Equal("Please review this code", content.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithEmptyString_WrapsAsUserTextMessage()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new SimplePromptReturnValueBinder(executionContext);

        await binder.SetValueAsync(string.Empty, CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        var content = Assert.IsType<TextContentBlock>(result.Messages[0].Content);
        Assert.Equal(string.Empty, content.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithNonStringValue_UsesToString()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new SimplePromptReturnValueBinder(executionContext);

        await binder.SetValueAsync(42, CancellationToken.None);

        var result = await executionContext.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Messages);
        var content = Assert.IsType<TextContentBlock>(result.Messages[0].Content);
        Assert.Equal("42", content.Text);
    }

    [Fact]
    public async Task GetValueAsync_ThrowsNotSupportedException()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new SimplePromptReturnValueBinder(executionContext);

        await Assert.ThrowsAsync<NotSupportedException>(() => binder.GetValueAsync());
    }

    [Fact]
    public void ToInvokeString_ReturnsEmptyString()
    {
        var executionContext = GetPromptExecutionContextHelper.CreateExecutionContext();
        var binder = new SimplePromptReturnValueBinder(executionContext);

        Assert.Equal(string.Empty, binder.ToInvokeString());
    }
}
