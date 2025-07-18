// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Moq;
using Microsoft.Azure.Functions.Extensions.Mcp.Http;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpHttpUtilityTests
{
    [Fact]
    public void TryGetFunctionKey_ReturnsTrue_WhenQueryContainsFunctionKey()
    {
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { McpConstants.FunctionsCodeQuery, "function-key" }
        });

        var result = McpHttpUtility.TryGetFunctionKey(context, out var code);

        Assert.True(result);
        Assert.Equal("function-key", code);
    }

    [Fact]
    public void TryGetFunctionKey_ReturnsTrue_WhenHeaderContainsFunctionKey()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[McpConstants.FunctionsKeyHeader] = "function-key";

        var result = McpHttpUtility.TryGetFunctionKey(context, out var code);

        Assert.True(result);
        Assert.Equal("function-key", code);
    }

    [Fact]
    public void TryGetFunctionKey_ReturnsFalse_WhenNoFunctionKeyExists()
    {
        var context = new DefaultHttpContext();

        var result = McpHttpUtility.TryGetFunctionKey(context, out var code);

        Assert.False(result);
        Assert.Null(code);
    }

    [Fact]
    public void SetSseContext_SetsCorrectHeaders()
    {
        var context = new DefaultHttpContext();
        var responseFeatureMock = new Mock<IHttpResponseBodyFeature>();
        context.Features.Set(responseFeatureMock.Object);

        McpHttpUtility.SetSseContext(context);

        Assert.Equal("text/event-stream", context.Response.Headers.ContentType);
        Assert.Equal("no-cache,no-store", context.Response.Headers.CacheControl);
        Assert.Equal("keep-alive", context.Response.Headers.Connection);
        Assert.Equal("identity", context.Response.Headers.ContentEncoding);

        responseFeatureMock.Verify(feature => feature.DisableBuffering(), Times.Once);
    }
}
