// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests;

public class SafePathResolverTests : IDisposable
{
    private readonly string _tempRoot;

    public SafePathResolverTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "SafePathResolverTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private string CreateTempFile(string relativePath)
    {
        var fullPath = Path.Combine(_tempRoot, relativePath);
        var dir = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, "test");
        return fullPath;
    }

    [Fact]
    public void Resolve_ValidPath_ReturnsFullPath()
    {
        var expected = CreateTempFile("app.html");

        var result = SafePathResolver.Resolve("app.html", _tempRoot);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_DotDotTraversal_ReturnsNull()
    {
        var result = SafePathResolver.Resolve("../../../etc/passwd", _tempRoot);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_UrlEncodedTraversal_ReturnsNull()
    {
        var result = SafePathResolver.Resolve("%2e%2e%2fetc%2fpasswd", _tempRoot);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_NullByteInjection_ReturnsNull()
    {
        var result = SafePathResolver.Resolve("file.html\0.jpg", _tempRoot);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_BackslashTraversal_ReturnsNull()
    {
        var result = SafePathResolver.Resolve("..\\..\\etc\\passwd", _tempRoot);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_NonExistentFile_ReturnsNull()
    {
        var result = SafePathResolver.Resolve("nonexistent.html", _tempRoot);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_MapFile_DefaultOptions_ReturnsNull()
    {
        CreateTempFile("app.js.map");

        var result = SafePathResolver.Resolve("app.js.map", _tempRoot);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_MapFile_IncludeSourceMaps_ReturnsPath()
    {
        var expected = CreateTempFile("app.js.map");
        var options = new StaticAssetOptions { IncludeSourceMaps = true };

        var result = SafePathResolver.Resolve("app.js.map", _tempRoot, options);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_EmptyPath_ReturnsNull()
    {
        var result = SafePathResolver.Resolve(string.Empty, _tempRoot);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_AbsolutePathOutsideRoot_ReturnsNull()
    {
        var result = SafePathResolver.Resolve("C:\\Windows\\System32\\cmd.exe", _tempRoot);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_SubdirectoryPath_ReturnsFullPath()
    {
        var expected = CreateTempFile(Path.Combine("sub", "dir", "file.html"));

        var result = SafePathResolver.Resolve("sub/dir/file.html", _tempRoot);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_WhitespacePath_ReturnsNull()
    {
        var result = SafePathResolver.Resolve("   ", _tempRoot);

        Assert.Null(result);
    }
}
