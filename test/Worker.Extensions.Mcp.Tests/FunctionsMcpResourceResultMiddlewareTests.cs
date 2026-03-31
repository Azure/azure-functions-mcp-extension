// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Moq;
using static Worker.Extensions.Mcp.Tests.Helpers.FunctionContextHelper;

namespace Worker.Extensions.Mcp.Tests;

public class FunctionsMcpResourceResultMiddlewareTests : IDisposable
{
    private object? _currentResult;
    private readonly FunctionsMcpResourceResultMiddleware _middleware;
    private readonly string _tempDir;

    public FunctionsMcpResourceResultMiddlewareTests()
    {
        _middleware = new FunctionsMcpResourceResultMiddleware(
            getResult: _ => _currentResult,
            setResult: (_, value) => _currentResult = value);
        _tempDir = Path.Combine(Path.GetTempPath(), $"mcp-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region Non-FileResourceContents passthrough

    [Fact]
    public async Task Invoke_WithNonMcpResourceContext_DoesNotModifyResult()
    {
        var context = CreateEmptyFunctionContext();
        var originalValue = "original value";
        _currentResult = originalValue;
        var nextCalled = false;

        await _middleware.Invoke(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Equal(originalValue, _currentResult);
    }

    [Fact]
    public async Task Invoke_WithNullResult_DoesNotModifyResult()
    {
        var context = CreateResourceContext();
        _currentResult = null;

        await _middleware.Invoke(context, _ => Task.CompletedTask);

        Assert.Null(_currentResult);
    }

    [Fact]
    public async Task Invoke_WithStringResult_DoesNotModifyResult()
    {
        var context = CreateResourceContext();
        var originalValue = "plain text resource";

        await _middleware.Invoke(context, _ =>
        {
            _currentResult = originalValue;
            return Task.CompletedTask;
        });

        Assert.Equal(originalValue, _currentResult);
    }

    #endregion

    #region Text file reading

    [Fact]
    public async Task Invoke_WithTextFile_ReturnsString()
    {
        var filePath = CreateTempFile("hello.txt", "Hello, World!");
        var context = CreateResourceContext(mimeType: "text/plain");

        await _middleware.Invoke(context, _ =>
        {
            _currentResult = new FileResourceContents { Path = filePath };
            return Task.CompletedTask;
        });

        Assert.IsType<string>(_currentResult);
        Assert.Equal("Hello, World!", _currentResult);
    }

    [Fact]
    public async Task Invoke_WithHtmlFile_ReturnsString()
    {
        var html = "<html><body>Hello</body></html>";
        var filePath = CreateTempFile("page.html", html);
        var context = CreateResourceContext(mimeType: "text/html");

        await _middleware.Invoke(context, _ =>
        {
            _currentResult = new FileResourceContents { Path = filePath };
            return Task.CompletedTask;
        });

        Assert.Equal(html, _currentResult);
    }

    [Fact]
    public async Task Invoke_WithJsonFile_ReturnsString()
    {
        var json = """{"key": "value"}""";
        var filePath = CreateTempFile("data.json", json);
        var context = CreateResourceContext(mimeType: "application/json");

        await _middleware.Invoke(context, _ =>
        {
            _currentResult = new FileResourceContents { Path = filePath };
            return Task.CompletedTask;
        });

        Assert.Equal(json, _currentResult);
    }

    [Theory]
    [InlineData("application/xml")]
    [InlineData("application/javascript")]
    [InlineData("text/css")]
    [InlineData("text/html+skybridge")]
    [InlineData("application/vnd.api+json")]
    [InlineData("application/soap+xml")]
    public async Task Invoke_WithTextMimeTypes_ReturnsString(string mimeType)
    {
        var content = "text content for " + mimeType;
        var filePath = CreateTempFile("file.txt", content);
        var context = CreateResourceContext(mimeType: mimeType);

        await _middleware.Invoke(context, _ =>
        {
            _currentResult = new FileResourceContents { Path = filePath };
            return Task.CompletedTask;
        });

        Assert.IsType<string>(_currentResult);
        Assert.Equal(content, _currentResult);
    }

    [Fact]
    public async Task Invoke_WithNoMimeType_DefaultsToText()
    {
        var filePath = CreateTempFile("noext", "some content");
        var context = CreateResourceContext();

        await _middleware.Invoke(context, _ =>
        {
            _currentResult = new FileResourceContents { Path = filePath };
            return Task.CompletedTask;
        });

        Assert.IsType<string>(_currentResult);
        Assert.Equal("some content", _currentResult);
    }

    #endregion

    #region Binary file reading

    [Fact]
    public async Task Invoke_WithBinaryFile_ReturnsByteArray()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var filePath = CreateTempBinaryFile("image.png", bytes);
        var context = CreateResourceContext(mimeType: "image/png");

        await _middleware.Invoke(context, _ =>
        {
            _currentResult = new FileResourceContents { Path = filePath };
            return Task.CompletedTask;
        });

        Assert.IsType<byte[]>(_currentResult);
        Assert.Equal(bytes, (byte[])_currentResult!);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    [InlineData("application/octet-stream")]
    [InlineData("audio/mpeg")]
    [InlineData("video/mp4")]
    public async Task Invoke_WithBinaryMimeTypes_ReturnsByteArray(string mimeType)
    {
        var bytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var filePath = CreateTempBinaryFile("file.bin", bytes);
        var context = CreateResourceContext(mimeType: mimeType);

        await _middleware.Invoke(context, _ =>
        {
            _currentResult = new FileResourceContents { Path = filePath };
            return Task.CompletedTask;
        });

        Assert.IsType<byte[]>(_currentResult);
        Assert.Equal(bytes, (byte[])_currentResult!);
    }

    #endregion

    #region Path resolution

    [Fact]
    public void ResolvePath_WithAbsolutePath_ReturnsAsIs()
    {
        var absolutePath = Path.Combine(_tempDir, "test.txt");
        Assert.Equal(absolutePath, FunctionsMcpResourceResultMiddleware.ResolvePath(absolutePath));
    }

    [Fact]
    public void ResolvePath_WithRelativePath_ResolvesAgainstBaseDirectory()
    {
        var result = FunctionsMcpResourceResultMiddleware.ResolvePath(Path.Combine("assets", "logo.png"));
        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "assets", "logo.png"), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolvePath_WithNullOrEmptyPath_ThrowsArgumentException(string? path)
    {
        Assert.Throws<ArgumentException>(() => FunctionsMcpResourceResultMiddleware.ResolvePath(path!));
    }

    #endregion

    #region Error handling

    [Fact]
    public async Task Invoke_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var context = CreateResourceContext(mimeType: "text/plain");

        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await _middleware.Invoke(context, _ =>
            {
                _currentResult = new FileResourceContents
                {
                    Path = Path.Combine(_tempDir, "does-not-exist.txt")
                };
                return Task.CompletedTask;
            });
        });
    }

    #endregion

    #region MimeTypeHelper tests

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("text/plain", true)]
    [InlineData("text/html", true)]
    [InlineData("text/css", true)]
    [InlineData("text/html+skybridge", true)]
    [InlineData("application/json", true)]
    [InlineData("application/xml", true)]
    [InlineData("application/javascript", true)]
    [InlineData("application/vnd.api+json", true)]
    [InlineData("application/soap+xml", true)]
    [InlineData("image/png", false)]
    [InlineData("image/jpeg", false)]
    [InlineData("application/pdf", false)]
    [InlineData("application/octet-stream", false)]
    [InlineData("audio/mpeg", false)]
    [InlineData("video/mp4", false)]
    public void IsTextMimeType_ReturnsExpectedResult(string? mimeType, bool expected)
    {
        Assert.Equal(expected, MimeTypeHelper.IsTextMimeType(mimeType));
    }

    #endregion

    #region Helpers

    private string CreateTempFile(string fileName, string content)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    private string CreateTempBinaryFile(string fileName, byte[] content)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllBytes(filePath, content);
        return filePath;
    }

    private static FunctionContext CreateResourceContext(string? mimeType = null)
    {
        var bindingData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (mimeType is not null)
        {
            bindingData["MimeType"] = mimeType;
        }

        var items = new Dictionary<object, object?>
        {
            { Constants.ResourceInvocationContextKey, new ResourceInvocationContext("test://resource") }
        };

        var bindingContextMock = new Mock<BindingContext>();
        bindingContextMock.SetupGet(b => b.BindingData).Returns(bindingData!);

        var contextMock = new Mock<FunctionContext>();
        contextMock.SetupGet(c => c.Items).Returns(items!);
        contextMock.SetupGet(c => c.BindingContext).Returns(bindingContextMock.Object);

        return contextMock.Object;
    }

    #endregion
}
