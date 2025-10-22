// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpToolTriggerRichContentReturnValueBinderTest
{
    [Fact]
    public async Task SetValueAsync_SetsStructuredContent_WhenTypeIsRaw()
    {
        // Arrange
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerRichContentReturnValueBinder(context);

        var rawJson = "{ \"jsonrpc\": \"2.0\", \"id\": 1, \"result\": {} }";
        var mcpToolResult = new McpToolResult
        {
            Type = McpContentToolType.Raw,
            Content = rawJson
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        // Act
        await binder.SetValueAsync(json, CancellationToken.None);

        // Assert
        var result = Assert.IsType<CallToolResult>(await context.ResultTask);

        // Content should be empty
        Assert.Empty(result.Content);

        // StructuredContent should contain the raw JSON string
        Assert.NotNull(result.StructuredContent);
        Assert.Equal(rawJson, result.StructuredContent!.ToString());
    }

    [Fact]
    public async Task SetValueAsync_SetsTextContentBlock_WhenTypeIsText()
    {
        // Arrange
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerRichContentReturnValueBinder(context);
        var input = "hello";

        var mcpToolResult = new McpToolResult
        {
            Type = McpContentToolType.Text,
            Content = JsonSerializer.Serialize(new TextContentBlock { Text = input })
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        // Act
        await binder.SetValueAsync(json, CancellationToken.None);

        // Assert
        var result = Assert.IsType<CallToolResult>(await context.ResultTask);
        var textBlock = Assert.Single(result.Content) as TextContentBlock;
        Assert.NotNull(textBlock);
        Assert.Equal("text", textBlock!.Type);
        Assert.Equal(input, textBlock.Text);
    }

    [Fact]
    public async Task SetValueAsync_SetsAudioContentBlock_WhenTypeIsAudio()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerRichContentReturnValueBinder(context);

        var data = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var innerBlock = new AudioContentBlock { Data = data, MimeType = "audio/mpeg", };
        var mcpToolResult = new McpToolResult
        {
            Type = McpContentToolType.Audio,
            Content = JsonSerializer.Serialize(innerBlock)
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        await binder.SetValueAsync(json, CancellationToken.None);

        var result = Assert.IsType<CallToolResult>(await context.ResultTask);
        var audioBlock = Assert.Single(result.Content) as AudioContentBlock;
        Assert.NotNull(audioBlock);
        Assert.Equal(innerBlock.MimeType, audioBlock!.MimeType);
        Assert.Equal(innerBlock.Data, audioBlock.Data);
    }

    [Fact]
    public async Task SetValueAsync_SetsImageContentBlock_WhenTypeIsImage()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerRichContentReturnValueBinder(context);

        var data = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var innerBlock = new ImageContentBlock { Data = data, MimeType = "image/png", };
        var mcpToolResult = new McpToolResult
        {
            Type = McpContentToolType.Image,
            Content = JsonSerializer.Serialize(innerBlock)
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        await binder.SetValueAsync(json, CancellationToken.None);

        var result = Assert.IsType<CallToolResult>(await context.ResultTask);
        var imageBlock = Assert.Single(result.Content) as ImageContentBlock;
        Assert.NotNull(imageBlock);
        Assert.Equal(innerBlock.MimeType, imageBlock!.MimeType);
        Assert.Equal(innerBlock.Data, imageBlock.Data);
    }

    [Fact]
    public async Task SetValueAsync_SetsResourceLinkBlock_WhenTypeIsResourceLink()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerRichContentReturnValueBinder(context);

        var innerBlock = new ResourceLinkBlock { Uri = "https://example.com", MimeType = "text/html", Name = "Example" };
        var mcpToolResult = new McpToolResult
        {
            Type = McpContentToolType.ResourceLink,
            Content = JsonSerializer.Serialize(innerBlock)
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        await binder.SetValueAsync(json, CancellationToken.None);

        var result = Assert.IsType<CallToolResult>(await context.ResultTask);
        var linkBlock = Assert.Single(result.Content) as ResourceLinkBlock;
        Assert.NotNull(linkBlock);
        Assert.Equal(innerBlock.Uri, linkBlock!.Uri);
        Assert.Equal(innerBlock.MimeType, linkBlock.MimeType);
        Assert.Equal(innerBlock.Name, linkBlock.Name);
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
    public async Task SetValueAsync_ThrowsArgumentException_WhenValueIsNotString()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerRichContentReturnValueBinder(context);

        await Assert.ThrowsAsync<ArgumentException>(() => binder.SetValueAsync(123, CancellationToken.None));
    }

    [Fact]
    public async Task SetValueAsync_ThrowsInvalidOperationException_WhenContentIsMissing()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerRichContentReturnValueBinder(context);

        var mcpToolResult = new McpToolResult
        {
            Type = McpContentToolType.Text,
            Content = ""
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        await Assert.ThrowsAsync<InvalidOperationException>(() => binder.SetValueAsync(json, CancellationToken.None));
    }

    [Fact]
    public async Task SetValueAsync_ThrowsInvalidOperationException_WhenContentIsWhitespace()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerRichContentReturnValueBinder(context);

        var mcpToolResult = new McpToolResult
        {
            Type = McpContentToolType.Text,
            Content = "   "
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        await Assert.ThrowsAsync<InvalidOperationException>(() => binder.SetValueAsync(json, CancellationToken.None));
    }

    [Fact]
    public async Task SetValueAsync_ThrowsInvalidOperationException_WhenTypeIsUnsupported()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerRichContentReturnValueBinder(context);

        var mcpToolResult = new McpToolResult
        {
            Type = (McpContentToolType)999, // unsupported
            Content = "ignored"
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        await Assert.ThrowsAsync<InvalidOperationException>(() => binder.SetValueAsync(json, CancellationToken.None));
    }

    [Fact]
    public async Task SetValueAsync_ThrowsInvalidOperationException_WhenInnerContentIsMalformed()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerRichContentReturnValueBinder(context);

        var mcpToolResult = new McpToolResult
        {
            Type = McpContentToolType.Text,
            Content = "{invalid-json}"
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => binder.SetValueAsync(json, CancellationToken.None));
        Assert.Contains("Failed to deserialize content", ex.Message);
    }

    [Fact]
    public async Task GetValueAsync_ThrowsNotSupportedException()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerRichContentReturnValueBinder(context);

        await Assert.ThrowsAsync<NotSupportedException>(binder.GetValueAsync);
    }

    [Fact]
    public void ToInvokeString_ReturnsEmptyString()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new McpToolTriggerRichContentReturnValueBinder(context);

        Assert.Equal(string.Empty, binder.ToInvokeString());
    }
}
