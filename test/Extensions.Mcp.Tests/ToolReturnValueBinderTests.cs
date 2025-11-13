// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class ToolReturnValueBinderTests
{
    [Fact]
    public async Task SetValueAsync_SetsTextContentBlock_WhenTypeIsText()
    {
        // Arrange
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(context);
        var input = "hello";

        var mcpToolResult = new McpToolResult
        {
            Type = "text",
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
        var binder = new ToolReturnValueBinder(context);

        var data = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var innerBlock = new AudioContentBlock { Data = data, MimeType = "audio/mpeg", };
        var mcpToolResult = new McpToolResult
        {
            Type = "audio",
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
        var binder = new ToolReturnValueBinder(context);

        var data = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var innerBlock = new ImageContentBlock { Data = data, MimeType = "image/png", };
        var mcpToolResult = new McpToolResult
        {
            Type = "image",
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
    public async Task SetValueAsync_SetsEmbeddedResourceBlock_WhenTypeIsResource()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(context);

        var innerBlock = new EmbeddedResourceBlock
        {
            Resource = new TextResourceContents
            {
                Uri = "urn:example",
                MimeType = "text/plain",
            },
        };
        var mcpToolResult = new McpToolResult
        {
            Type = "resource",
            Content = JsonSerializer.Serialize(innerBlock)
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        await binder.SetValueAsync(json, CancellationToken.None);

        var result = Assert.IsType<CallToolResult>(await context.ResultTask);
        var resourceBlock = Assert.Single(result.Content) as EmbeddedResourceBlock;
        Assert.NotNull(resourceBlock);
        Assert.Equal(innerBlock.Resource.Uri, resourceBlock.Resource.Uri);
        Assert.Equal(innerBlock.Resource.MimeType, resourceBlock.Resource.MimeType);
    }

    [Fact]
    public async Task SetValueAsync_SetsResourceLinkBlock_WhenTypeIsResourceLink()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(context);

        var innerBlock = new ResourceLinkBlock
        {
            Uri = "https://example.com",
            Name = "Example",
            Size = 1234
        };

        var mcpToolResult = new McpToolResult
        {
            Type = "resource_link",
            Content = JsonSerializer.Serialize(innerBlock)
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        await binder.SetValueAsync(json, CancellationToken.None);

        var result = Assert.IsType<CallToolResult>(await context.ResultTask);
        var linkBlock = Assert.Single(result.Content) as ResourceLinkBlock;
        Assert.NotNull(linkBlock);
        Assert.Equal(innerBlock.Uri, linkBlock.Uri);
        Assert.Equal(innerBlock.Name, linkBlock.Name);
    }

    [Fact]
    public async Task SetValueAsync_SetsMultipleBlocks_WhenTypeIsMultiContentResult_WithMixedTypes()
    {
        // Arrange
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(context);

        string contentBlocksJson = "[{\"type\":\"text\",\"text\":\"hello\"},{\"type\":\"image\",\"data\":\"AQID\",\"mimeType\":\"image/png\"},{\"type\":\"resource_link\",\"uri\":\"https://example.com/resource\",\"name\":\"Test Resource\"}]";

        var mcpToolResult = new McpToolResult
        {
            Type = "multi_content_result",
            Content = contentBlocksJson
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        // Act
        await binder.SetValueAsync(json, CancellationToken.None);

        // Assert
        var result = Assert.IsType<CallToolResult>(await context.ResultTask);
        Assert.Equal(3, result.Content.Count);

        // Validate order & types
        Assert.IsType<TextContentBlock>(result.Content[0]);
        Assert.IsType<ImageContentBlock>(result.Content[1]);
        Assert.IsType<ResourceLinkBlock>(result.Content[2]);

        var textBlock = (TextContentBlock)result.Content[0];
        Assert.Equal("text", textBlock.Type);
        Assert.Equal("hello", textBlock.Text);

        var imageBlock = (ImageContentBlock)result.Content[1];
        Assert.Equal("image/png", imageBlock.MimeType);
        Assert.Equal("AQID", imageBlock.Data);

        var linkBlock = (ResourceLinkBlock)result.Content[2];
        Assert.Equal("https://example.com/resource", linkBlock.Uri);
        Assert.Equal("Test Resource", linkBlock.Name);
    }

    [Fact]
    public async Task SetValueAsync_SetsMultipleImageBlocks_WhenArrayPayloadWithSingleType()
    {
        // Arrange
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(context);

        var img1 = new ImageContentBlock
        {
            Data = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            MimeType = "image/png"
        };
        var img2 = new ImageContentBlock
        {
            Data = Convert.ToBase64String(new byte[] { 9, 8, 7, 6 }),
            MimeType = "image/jpeg"
        };

        // Content is an array of ImageContentBlock
        var mcpToolResult = new McpToolResult
        {
            Type = "multi_content_result",
            Content = JsonSerializer.Serialize(new[] { img1, img2 })
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        // Act
        await binder.SetValueAsync(json, CancellationToken.None);

        // Assert
        var result = Assert.IsType<CallToolResult>(await context.ResultTask);
        Assert.Equal(2, result.Content.Count);

        var first = Assert.IsType<ImageContentBlock>(result.Content[0]);
        var second = Assert.IsType<ImageContentBlock>(result.Content[1]);

        Assert.Equal(img1.MimeType, first.MimeType);
        Assert.Equal(img1.Data, first.Data);

        Assert.Equal(img2.MimeType, second.MimeType);
        Assert.Equal(img2.Data, second.Data);
    }

    [Fact]
    public async Task SetValueAsync_SetsNullResult_WhenValueIsNull()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(context);

        await binder.SetValueAsync(null!, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.Null(result);
    }

    [Fact]
    public async Task SetValueAsync_ThrowsArgumentException_WhenValueIsNotString()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(context);

        await Assert.ThrowsAsync<ArgumentException>(() => binder.SetValueAsync(123, CancellationToken.None));
    }

    [Fact]
    public async Task SetValueAsync_ThrowsArgumentException_WhenContentIsNull()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(context);

        var mcpToolResult = new McpToolResult
        {
            Type = "text",
            Content = null
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        await Assert.ThrowsAsync<ArgumentNullException>(() => binder.SetValueAsync(json, CancellationToken.None));
    }

    [Fact]
    public async Task SetValueAsync_ThrowsInvalidOperationException_WhenTypeIsUnsupported()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(context);

        var mcpToolResult = new McpToolResult
        {
            Type = "random_type", // unsupported
            Content = "ignored"
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        await Assert.ThrowsAsync<InvalidOperationException>(() => binder.SetValueAsync(json, CancellationToken.None));
    }

    [Fact]
    public async Task SetValueAsync_ThrowsInvalidOperationException_WhenInnerContentIsMalformed()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(context);

        var mcpToolResult = new McpToolResult
        {
            Type = "text",
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
        var binder = new ToolReturnValueBinder(context);

        await Assert.ThrowsAsync<NotSupportedException>(binder.GetValueAsync);
    }

    [Fact]
    public void ToInvokeString_ReturnsEmptyString()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(context);

        Assert.Equal(string.Empty, binder.ToInvokeString());
    }

    [Fact]
    public async Task SetValueAsync_ThrowsInvalidOperation_WhenMultiContent_EmptyArray()
    {
        var context = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(context);

        var mcpToolResult = new McpToolResult
        {
            Type = "multi_content_result",
            Content = JsonSerializer.Serialize(Array.Empty<object>())
        };
        var json = JsonSerializer.Serialize(mcpToolResult);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => binder.SetValueAsync(json, CancellationToken.None));
        Assert.Contains("No content items were produced", ex.Message);
    }

    [Fact]
    public async Task SetValueAsync_ThrowsJsonException_WhenInputIsNotJson()
    {
        var ctx = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(ctx);

        await Assert.ThrowsAsync<JsonException>(() => binder.SetValueAsync("not-json", CancellationToken.None));
    }

    [Fact]
    public async Task SetValueAsync_ThrowsInvalidOperation_WhenTextContentIsNullLiteral()
    {
        var ctx = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(ctx);

        var result = new McpToolResult { Type = "text", Content = "null" };
        var json = JsonSerializer.Serialize(result);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => binder.SetValueAsync(json, CancellationToken.None));
        Assert.Contains("Failed to deserialize content block type 'text'", ex.Message);
    }

    [Fact]
    public async Task SetValueAsync_ThrowsInvalidOperation_WhenMultiContentContainsUnknownType()
    {
        var ctx = CallToolExecutionContextHelper.CreateExecutionContext();
        var binder = new ToolReturnValueBinder(ctx);

        var content = new object[] {
        new TextContentBlock { Text = "ok" },
        new { type = "unknown_type", foo = 1 }
    };
        var json = JsonSerializer.Serialize(new McpToolResult
        {
            Type = "multi_content_result",
            Content = JsonSerializer.Serialize(content)
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => binder.SetValueAsync(json, CancellationToken.None));
        Assert.Contains("Failed to deserialize content for type 'multi_content_result'", ex.Message);
    }
}
