// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace Worker.Extensions.Mcp.Tests;

public class MimeTypeHelperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsTextMimeType_WithNullOrEmpty_ReturnsTrue(string? mimeType)
    {
        Assert.True(MimeTypeHelper.IsTextMimeType(mimeType));
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/html")]
    [InlineData("text/css")]
    [InlineData("text/csv")]
    [InlineData("text/markdown")]
    [InlineData("TEXT/PLAIN")]
    public void IsTextMimeType_WithTextTypes_ReturnsTrue(string mimeType)
    {
        Assert.True(MimeTypeHelper.IsTextMimeType(mimeType));
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/xml")]
    [InlineData("application/javascript")]
    [InlineData("APPLICATION/JSON")]
    public void IsTextMimeType_WithTextApplicationTypes_ReturnsTrue(string mimeType)
    {
        Assert.True(MimeTypeHelper.IsTextMimeType(mimeType));
    }

    [Theory]
    [InlineData("application/vnd.api+json")]
    [InlineData("application/soap+xml")]
    [InlineData("application/ld+json")]
    [InlineData("application/atom+xml")]
    public void IsTextMimeType_WithStructuredSyntaxSuffixes_ReturnsTrue(string mimeType)
    {
        Assert.True(MimeTypeHelper.IsTextMimeType(mimeType));
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    [InlineData("application/octet-stream")]
    [InlineData("application/zip")]
    [InlineData("audio/mpeg")]
    [InlineData("video/mp4")]
    public void IsTextMimeType_WithBinaryTypes_ReturnsFalse(string mimeType)
    {
        Assert.False(MimeTypeHelper.IsTextMimeType(mimeType));
    }
}
