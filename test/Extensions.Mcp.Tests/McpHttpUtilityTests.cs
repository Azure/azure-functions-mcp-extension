// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Functions.Extensions.Mcp.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Extensions.Primitives;
using ModelContextProtocol.Protocol;
using Moq;
using System.Text;

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

    [Fact]
    public async Task ExtractJsonRpcMessageAsync_UnwrapsWrapper()
    {
        var wrapper = "{ \"isFunctionsMcpResult\": true, \"content\": { \"jsonrpc\": \"2.0\", \"method\": \"test\" } }";

        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(wrapper);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;

        var message = await McpHttpUtility.ExtractJsonRpcMessageSseAsync(context.Request, McpJsonSerializerOptions.DefaultOptions, default);

        Assert.NotNull(message);
        Assert.IsType<JsonRpcNotification>(message);
        var notification = (JsonRpcNotification)message!;
        Assert.Equal("test", notification.Method);
    }

    [Fact]
    public async Task ExtractJsonRpcMessageAsync_ParsesRawJsonRpc()
    {
        var raw = "{ \"jsonrpc\": \"2.0\", \"method\": \"raw\" }";

        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(raw);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;

        var message = await McpHttpUtility.ExtractJsonRpcMessageSseAsync(context.Request, McpJsonSerializerOptions.DefaultOptions, default);

        Assert.NotNull(message);
        Assert.IsType<JsonRpcNotification>(message);
        var notification = (JsonRpcNotification)message!;
        Assert.Equal("raw", notification.Method);
    }

    [Fact]
    public async Task UnwrapFunctionsMcpPayloadAsync_ReplacesBodyWithInnerContent()
    {
        var wrapper = "{ \"isFunctionsMcpResult\": true, \"content\": { \"jsonrpc\": \"2.0\", \"method\": \"inner\" } }";

        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(wrapper);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;

        await McpHttpUtility.ExtractJsonRpcMessageHttpStreamableAsync(context.Request, default);

        using var sr = new StreamReader(context.Request.Body, Encoding.UTF8);
        context.Request.Body.Position = 0;
        var bodyText = await sr.ReadToEndAsync();

        Assert.Contains("\"method\": \"inner\"", bodyText);
        Assert.DoesNotContain("isFunctionsMcpResult", bodyText);
    }

    [Fact]
    public async Task ProcessJsonRpcPayloadAsync_ExtractMode_UnwrapsWrapper()
    {
        var wrapper = "{ \"isFunctionsMcpResult\": true, \"content\": { \"jsonrpc\": \"2.0\", \"method\": \"test\" } }";

        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(wrapper);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;

        var message = await McpHttpUtility.ProcessJsonRpcPayloadAsync(context.Request, McpJsonSerializerOptions.DefaultOptions, default, unwrapOnly: false);

        Assert.NotNull(message);
        Assert.IsType<JsonRpcNotification>(message);
        var notification = (JsonRpcNotification)message!;
        Assert.Equal("test", notification.Method);
    }

    [Fact]
    public async Task ProcessJsonRpcPayloadAsync_UnwrapMode_ReplacesBodyWithInnerContent()
    {
        var wrapper = "{ \"isFunctionsMcpResult\": true, \"content\": { \"jsonrpc\": \"2.0\", \"method\": \"inner\" } }";

        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(wrapper);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;

        var result = await McpHttpUtility.ProcessJsonRpcPayloadAsync(context.Request, McpJsonSerializerOptions.DefaultOptions, default, unwrapOnly: true);

        Assert.Null(result); // unwrapOnly mode returns null

        using var sr = new StreamReader(context.Request.Body, Encoding.UTF8);
        context.Request.Body.Position = 0;
        var bodyText = await sr.ReadToEndAsync();

        Assert.Contains("\"method\": \"inner\"", bodyText);
        Assert.DoesNotContain("isFunctionsMcpResult", bodyText);
    }

    [Fact]
    public async Task ProcessJsonRpcPayloadAsync_EmptyBody_ReturnsNull()
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream();
        context.Request.ContentLength = 0;

        var result = await McpHttpUtility.ProcessJsonRpcPayloadAsync(context.Request, McpJsonSerializerOptions.DefaultOptions, default, unwrapOnly: false);

        Assert.Null(result);
    }

    [Fact]
    public async Task ProcessJsonRpcPayloadAsync_UnwrapMode_NoWrapper_LeavesBodyIntact()
    {
        var raw = "{ \"jsonrpc\": \"2.0\", \"method\": \"raw\" }";

        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(raw);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;

        var result = await McpHttpUtility.ProcessJsonRpcPayloadAsync(context.Request, McpJsonSerializerOptions.DefaultOptions, default, unwrapOnly: true);

        Assert.Null(result); // unwrapOnly mode returns null

        using var sr = new StreamReader(context.Request.Body, Encoding.UTF8);
        context.Request.Body.Position = 0;
        var bodyText = await sr.ReadToEndAsync();

        Assert.Equal(raw, bodyText);
        Assert.DoesNotContain("isFunctionsMcpResult", bodyText);
    }
}
