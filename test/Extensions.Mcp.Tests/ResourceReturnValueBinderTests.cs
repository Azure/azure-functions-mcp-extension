// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class ResourceReturnValueBinderTests
{
    private static (ResourceReturnValueBinder binder, ReadResourceExecutionContext context) CreateBinder(
        string uri = "test://resource/1",
        string? mimeType = null)
    {
        var context = ReadResourceExecutionContextHelper.CreateExecutionContext(uri);
        var attribute = new McpResourceTriggerAttribute(uri, "TestResource")
        {
            MimeType = mimeType
        };

        var binder = new ResourceReturnValueBinder(context, attribute, NullLogger<ResourceReturnValueBinder>.Instance);
        return (binder, context);
    }

    [Fact]
    public void Type_IsObject()
    {
        var (binder, _) = CreateBinder();
        Assert.Equal(typeof(object), binder.Type);
    }

    [Fact]
    public void ToInvokeString_ReturnsEmptyString()
    {
        var (binder, _) = CreateBinder();
        Assert.Equal(string.Empty, binder.ToInvokeString());
    }

    [Fact]
    public async Task GetValueAsync_ThrowsNotSupportedException()
    {
        var (binder, _) = CreateBinder();
        await Assert.ThrowsAsync<NotSupportedException>(() => binder.GetValueAsync());
    }

    [Fact]
    public async Task SetValueAsync_WithNull_SetsNullResult()
    {
        var (binder, context) = CreateBinder();

        await binder.SetValueAsync(null!, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.Null(result);
    }

    [Fact]
    public async Task SetValueAsync_WithSimpleString_CreatesTextResourceContents()
    {
        var (binder, context) = CreateBinder(uri: "test://resource/1", mimeType: "text/plain");

        await binder.SetValueAsync("Hello, World!", CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        
        var textContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.Equal("test://resource/1", textContent.Uri);
        Assert.Equal("text/plain", textContent.MimeType);
        Assert.Equal("Hello, World!", textContent.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithByteArray_CreatesBlobResourceContents()
    {
        var (binder, context) = CreateBinder(uri: "test://resource/1", mimeType: "application/octet-stream");
        var binaryData = new byte[] { 1, 2, 3, 4, 5 };

        await binder.SetValueAsync(binaryData, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        
        var blobContent = Assert.IsType<BlobResourceContents>(result.Contents[0]);
        Assert.Equal("test://resource/1", blobContent.Uri);
        Assert.Equal("application/octet-stream", blobContent.MimeType);
        Assert.Equal(Convert.ToBase64String(binaryData), blobContent.Blob);
    }

    [Fact]
    public async Task SetValueAsync_WithMcpResourceResult_TextType_DeserializesCorrectly()
    {
        var (binder, context) = CreateBinder(uri: "test://resource/1", mimeType: "text/plain");

        var textContent = new TextResourceContents
        {
            Uri = "test://resource/1",
            Text = "Custom content",
            MimeType = "text/html"
        };

        var mcpResult = new McpResourceResult
        {
            Content = JsonSerializer.Serialize(textContent, McpJsonSerializerOptions.DefaultOptions)
        };

        var jsonString = JsonSerializer.Serialize(mcpResult, McpJsonSerializerOptions.DefaultOptions);

        await binder.SetValueAsync(jsonString, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        
        var resultContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.Equal("test://resource/1", resultContent.Uri);
        Assert.Equal("text/html", resultContent.MimeType);
        Assert.Equal("Custom content", resultContent.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithMcpResourceResult_BlobType_DeserializesCorrectly()
    {
        var (binder, context) = CreateBinder(uri: "test://resource/1", mimeType: "application/octet-stream");

        var binaryData = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var blobContent = new BlobResourceContents
        {
            Uri = "test://resource/1",
            Blob = binaryData,
            MimeType = "image/png"
        };

        var mcpResult = new McpResourceResult
        {
            Content = JsonSerializer.Serialize(blobContent, McpJsonSerializerOptions.DefaultOptions)
        };

        var jsonString = JsonSerializer.Serialize(mcpResult, McpJsonSerializerOptions.DefaultOptions);

        await binder.SetValueAsync(jsonString, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        
        var resultContent = Assert.IsType<BlobResourceContents>(result.Contents[0]);
        Assert.Equal("test://resource/1", resultContent.Uri);
        Assert.Equal("image/png", resultContent.MimeType);
        Assert.Equal(binaryData, resultContent.Blob);
    }

    [Fact]
    public async Task SetValueAsync_WithMcpResourceResult_EmptyUri_UsesAttributeUri()
    {
        var (binder, context) = CreateBinder(uri: "test://resource/attribute", mimeType: "text/plain");

        // When URI is not in JSON, TextResourceContents deserializes with empty string
        var contentJson = "{\"text\":\"Content without URI\",\"mimeType\":\"text/plain\"}";

        var mcpResult = new McpResourceResult
        {
            Content = contentJson
        };

        var jsonString = JsonSerializer.Serialize(mcpResult, McpJsonSerializerOptions.DefaultOptions);

        await binder.SetValueAsync(jsonString, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        
        var resultContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        // Empty URI is replaced with attribute URI
        Assert.Equal("test://resource/attribute", resultContent.Uri);
    }

    [Fact]
    public async Task SetValueAsync_WithMcpResourceResult_NullUri_UsesAttributeUri()
    {
        var (binder, context) = CreateBinder(uri: "test://resource/attribute", mimeType: "text/plain");

        // Manually create JSON with null uri to test fallback
        var contentJson = "{\"uri\":null,\"text\":\"Content with null URI\",\"mimeType\":\"text/plain\"}";

        var mcpResult = new McpResourceResult
        {
            Content = contentJson
        };

        var jsonString = JsonSerializer.Serialize(mcpResult, McpJsonSerializerOptions.DefaultOptions);

        await binder.SetValueAsync(jsonString, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        
        var resultContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        // Null URI is replaced with attribute URI via ??= operator
        Assert.Equal("test://resource/attribute", resultContent.Uri);
    }

    [Fact]
    public async Task SetValueAsync_WithMcpResourceResult_MissingMimeType_UsesAttributeMimeType()
    {
        var (binder, context) = CreateBinder(uri: "test://resource/1", mimeType: "text/xml");

        var textContent = new TextResourceContents
        {
            Uri = "test://resource/1",
            Text = "Content without MimeType",
            MimeType = null // Missing MimeType
        };

        var mcpResult = new McpResourceResult
        {
            Content = JsonSerializer.Serialize(textContent, McpJsonSerializerOptions.DefaultOptions)
        };

        var jsonString = JsonSerializer.Serialize(mcpResult, McpJsonSerializerOptions.DefaultOptions);

        await binder.SetValueAsync(jsonString, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        
        var resultContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.Equal("text/xml", resultContent.MimeType);
    }

    [Fact]
    public async Task SetValueAsync_WithInvalidJson_TreatsAsSimpleString()
    {
        var (binder, context) = CreateBinder(uri: "test://resource/1", mimeType: "text/plain");

        await binder.SetValueAsync("{ invalid json }", CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        
        var textContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.Equal("{ invalid json }", textContent.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithMcpResourceResult_MissingType_TreatsAsSimpleString()
    {
        var (binder, context) = CreateBinder(uri: "test://resource/1", mimeType: "text/plain");

        var mcpResult = new McpResourceResult
        {
            Content = "some content"
        };

        var jsonString = JsonSerializer.Serialize(mcpResult, McpJsonSerializerOptions.DefaultOptions);

        await binder.SetValueAsync(jsonString, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        
        var textContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.Equal(jsonString, textContent.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithUnsupportedType_Throws()
    {
        var (binder, context) = CreateBinder();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            binder.SetValueAsync(123, CancellationToken.None));
        
        Assert.Contains("Unsupported return type", exception.Message);
        Assert.Contains("Int32", exception.Message);
    }

    [Fact]
    public async Task SetValueAsync_WithEmptyByteArray_CreatesBlobResourceContents()
    {
        var (binder, context) = CreateBinder(uri: "test://resource/1", mimeType: "application/octet-stream");
        var binaryData = Array.Empty<byte>();

        await binder.SetValueAsync(binaryData, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        
        var blobContent = Assert.IsType<BlobResourceContents>(result.Contents[0]);
        Assert.Equal(Convert.ToBase64String(binaryData), blobContent.Blob);
    }

    [Fact]
    public async Task SetValueAsync_WithEmptyString_CreatesTextResourceContents()
    {
        var (binder, context) = CreateBinder(uri: "test://resource/1", mimeType: "text/plain");

        await binder.SetValueAsync("", CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        
        var textContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.Equal("", textContent.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithMcpResourceResult_NullContent_Throws()
    {
        var (binder, context) = CreateBinder(uri: "test://resource/1", mimeType: "text/plain");

        var mcpResult = new McpResourceResult
        {
            Content = "null" // Null content that deserializes to null
        };

        var jsonString = JsonSerializer.Serialize(mcpResult, McpJsonSerializerOptions.DefaultOptions);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            binder.SetValueAsync(jsonString, CancellationToken.None));
        
        Assert.Contains("Failed to deserialize", exception.Message);
    }

    [Fact]
    public async Task SetValueAsync_WithFileResourceContents_TextFile_CreatesTextResourceContents()
    {
        var testFilePath = Path.Combine(Path.GetTempPath(), "test-resources", "test.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(testFilePath)!);
        File.WriteAllText(testFilePath, "Hello, World! This is a test text file.");

        var (binder, context) = CreateBinder(uri: "test://resource/file", mimeType: "text/plain");

        var fileContents = new FileResourceContents
        {
            Path = testFilePath
        };

        await binder.SetValueAsync(fileContents, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);

        var textContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.Equal("test://resource/file", textContent.Uri);
        Assert.Equal("text/plain", textContent.MimeType);
        Assert.Equal("Hello, World! This is a test text file.", textContent.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithFileResourceContents_BinaryFile_CreatesBlobResourceContents()
    {
        var testFilePath = Path.Combine(Path.GetTempPath(), "test-resources", "test.png");
        Directory.CreateDirectory(Path.GetDirectoryName(testFilePath)!);
        var binaryData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        File.WriteAllBytes(testFilePath, binaryData);

        var (binder, context) = CreateBinder(uri: "test://resource/image", mimeType: "image/png");

        var fileContents = new FileResourceContents
        {
            Path = testFilePath
        };

        await binder.SetValueAsync(fileContents, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);

        var blobContent = Assert.IsType<BlobResourceContents>(result.Contents[0]);
        Assert.Equal("test://resource/image", blobContent.Uri);
        Assert.Equal("image/png", blobContent.MimeType);
        Assert.Equal(Convert.ToBase64String(binaryData), blobContent.Blob);
    }

    [Fact]
    public async Task SetValueAsync_WithFileResourceContents_JsonMimeType_CreatesTextResourceContents()
    {
        var testFilePath = Path.Combine(Path.GetTempPath(), "test-resources", "test.json");
        Directory.CreateDirectory(Path.GetDirectoryName(testFilePath)!);
        File.WriteAllText(testFilePath, "{\"test\": \"json content\"}");

        var (binder, context) = CreateBinder(uri: "test://resource/json", mimeType: "application/json");

        var fileContents = new FileResourceContents
        {
            Path = testFilePath
        };

        await binder.SetValueAsync(fileContents, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);

        var textContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.Equal("test://resource/json", textContent.Uri);
        Assert.Equal("application/json", textContent.MimeType);
        Assert.Equal("{\"test\": \"json content\"}", textContent.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithFileResourceContents_XmlMimeType_CreatesTextResourceContents()
    {
        var testFilePath = Path.Combine(Path.GetTempPath(), "test-resources", "test.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(testFilePath)!);
        File.WriteAllText(testFilePath, "<root><test>xml content</test></root>");

        var (binder, context) = CreateBinder(uri: "test://resource/xml", mimeType: "application/xml");

        var fileContents = new FileResourceContents
        {
            Path = testFilePath
        };

        await binder.SetValueAsync(fileContents, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);

        var textContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.Equal("application/xml", textContent.MimeType);
        Assert.Equal("<root><test>xml content</test></root>", textContent.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithFileResourceContents_CustomXmlPlusMimeType_CreatesTextResourceContents()
    {
        var testFilePath = Path.Combine(Path.GetTempPath(), "test-resources", "test-custom.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(testFilePath)!);
        File.WriteAllText(testFilePath, "<custom>content</custom>");

        var (binder, context) = CreateBinder(uri: "test://resource/custom", mimeType: "application/vnd.custom+xml");

        var fileContents = new FileResourceContents
        {
            Path = testFilePath
        };

        await binder.SetValueAsync(fileContents, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);

        var textContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.Equal("application/vnd.custom+xml", textContent.MimeType);
    }

    [Fact]
    public async Task SetValueAsync_WithFileResourceContents_FileNotFound_Throws()
    {
        var (binder, context) = CreateBinder(uri: "test://resource/missing", mimeType: "text/plain");

        var fileContents = new FileResourceContents
        {
            Path = "/nonexistent/path/to/file.txt"
        };

        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            binder.SetValueAsync(fileContents, CancellationToken.None));

        Assert.Contains("/nonexistent/path/to/file.txt", exception.Message);
    }

    [Fact]
    public async Task SetValueAsync_WithFileResourceContents_EmptyPath_Throws()
    {
        var (binder, context) = CreateBinder(uri: "test://resource/empty", mimeType: "text/plain");

        var fileContents = new FileResourceContents
        {
            Path = ""
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            binder.SetValueAsync(fileContents, CancellationToken.None));

        Assert.Contains("Path property", exception.Message);
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task SetValueAsync_WithFileResourceContents_WithCustomUri_UsesCustomUri()
    {
        var testFilePath = Path.Combine(Path.GetTempPath(), "test-resources", "custom-uri.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(testFilePath)!);
        File.WriteAllText(testFilePath, "Content with custom URI");

        var (binder, context) = CreateBinder(uri: "test://resource/default", mimeType: "text/plain");

        var fileContents = new FileResourceContents
        {
            Uri = "custom://resource/override",
            Path = testFilePath
        };

        await binder.SetValueAsync(fileContents, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);

        var textContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.Equal("custom://resource/override", textContent.Uri);
    }

    [Fact]
    public async Task SetValueAsync_WithFileResourceContents_WithCustomMimeType_UsesCustomMimeType()
    {
        var testFilePath = Path.Combine(Path.GetTempPath(), "test-resources", "custom-mime.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(testFilePath)!);
        File.WriteAllText(testFilePath, "Content with custom MIME type");

        var (binder, context) = CreateBinder(uri: "test://resource/mime", mimeType: "text/plain");

        var fileContents = new FileResourceContents
        {
            MimeType = "text/html",
            Path = testFilePath
        };

        await binder.SetValueAsync(fileContents, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);

        var textContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.Equal("text/html", textContent.MimeType);
    }

    [Fact]
    public async Task SetValueAsync_WithFileResourceContents_WithMeta_PreservesMeta()
    {
        var testFilePath = Path.Combine(Path.GetTempPath(), "test-resources", "meta.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(testFilePath)!);
        File.WriteAllText(testFilePath, "Content with metadata");

        var (binder, context) = CreateBinder(uri: "test://resource/meta", mimeType: "text/plain");

        var meta = new System.Text.Json.Nodes.JsonObject
        {
            ["author"] = "Test Author",
            ["version"] = 1
        };

        var fileContents = new FileResourceContents
        {
            Path = testFilePath,
            Meta = meta
        };

        await binder.SetValueAsync(fileContents, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);

        var textContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.NotNull(textContent.Meta);
        Assert.Equal("Test Author", textContent.Meta["author"]?.ToString());
        Assert.Equal(1, (int?)textContent.Meta["version"]);
    }

    [Fact]
    public async Task SetValueAsync_WithFileResourceContents_TextHtmlMimeType_CreatesTextResourceContents()
    {
        var testFilePath = Path.Combine(Path.GetTempPath(), "test-resources", "test.html");
        Directory.CreateDirectory(Path.GetDirectoryName(testFilePath)!);
        File.WriteAllText(testFilePath, "<html><body>Hello</body></html>");

        var (binder, context) = CreateBinder(uri: "test://resource/html", mimeType: "text/html");

        var fileContents = new FileResourceContents
        {
            Path = testFilePath
        };

        await binder.SetValueAsync(fileContents, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);

        var textContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.Equal("text/html", textContent.MimeType);
        Assert.Equal("<html><body>Hello</body></html>", textContent.Text);
    }

    [Fact]
    public async Task SetValueAsync_WithFileResourceContents_ApplicationJavaScriptMimeType_CreatesTextResourceContents()
    {
        var testFilePath = Path.Combine(Path.GetTempPath(), "test-resources", "test.js");
        Directory.CreateDirectory(Path.GetDirectoryName(testFilePath)!);
        File.WriteAllText(testFilePath, "console.log('test');");

        var (binder, context) = CreateBinder(uri: "test://resource/js", mimeType: "application/javascript");

        var fileContents = new FileResourceContents
        {
            Path = testFilePath
        };

        await binder.SetValueAsync(fileContents, CancellationToken.None);

        var result = await context.ResultTask;
        Assert.NotNull(result);
        Assert.Single(result.Contents);

        var textContent = Assert.IsType<TextResourceContents>(result.Contents[0]);
        Assert.Equal("application/javascript", textContent.MimeType);
    }
}
