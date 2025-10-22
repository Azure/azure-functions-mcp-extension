// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpToolTriggerReturnValueBinderTest
{

    [Fact]
    public async Task SetValueAsync_SetsTextContentBlock_WhenValueIsString()
    {
        // Arrange
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerReturnValueBinder(context);
        string input = "Hello MCP";

        // Act
        await binder.SetValueAsync(input, CancellationToken.None);

        // Assert
        var result = Assert.IsType<CallToolResult>(await context.ResultTask);
        var textBlock = Assert.Single(result.Content) as TextContentBlock;
        Assert.NotNull(textBlock);
        Assert.Equal("text", textBlock!.Type);
        Assert.Equal(input, textBlock.Text);
    }

    [Fact]
    public async Task SetValueAsync_SetsNullResult_WhenValueIsNull()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerReturnValueBinder(context);

        await binder.SetValueAsync(null!, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValueAsync_ThrowsNotSupportedException()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerReturnValueBinder(context);

        await Assert.ThrowsAsync<NotSupportedException>(binder.GetValueAsync);
    }

    [Fact]
    public void ToInvokeString_ReturnsEmptyString()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerReturnValueBinder(context);

        Assert.Equal(string.Empty, binder.ToInvokeString());
    }
}
