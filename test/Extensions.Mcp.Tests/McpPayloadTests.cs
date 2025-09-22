// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using ModelContextProtocol.Protocol;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpPayloadTests
{
    [Fact]
    public async Task ExtractJsonRpcMessageAsync_UnwrapsWrapper()
    {
        var wrapper = "{ \"isFunctionsMcpResult\": true, \"content\": { \"jsonrpc\": \"2.0\", \"method\": \"test\" } }";

        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(wrapper);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;

        var message = await McpHttpUtility.ExtractJsonRpcMessageAsync(context.Request, McpJsonSerializerOptions.DefaultOptions, default);

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

        var message = await McpHttpUtility.ExtractJsonRpcMessageAsync(context.Request, McpJsonSerializerOptions.DefaultOptions, default);

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

        await McpHttpUtility.UnwrapFunctionsMcpPayloadAsync(context.Request, default);

        using var sr = new StreamReader(context.Request.Body, Encoding.UTF8);
        context.Request.Body.Position = 0;
        var bodyText = await sr.ReadToEndAsync();

        Assert.Contains("\"method\": \"inner\"", bodyText);
        Assert.DoesNotContain("isFunctionsMcpResult", bodyText);
    }
}
