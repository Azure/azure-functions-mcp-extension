// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpTriggerTransportHelperTests
{
    [Fact]
    public void GetTransportInformation_WithHttpContextAccessor_UsesHeadersAndSession()
    {
        var services = new ServiceCollection();
        var httpContext = new DefaultHttpContext();

        httpContext.Items[McpConstants.McpTransportName] = "http-sse";
        httpContext.Request.Headers["X-Test"] = "abc";
        httpContext.Request.Headers[McpConstants.McpSessionIdHeaderName] = "session-123";

        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var serviceProvider = services.BuildServiceProvider();

        var transport = McpTriggerTransportHelper.GetTransportInformation(serviceProvider);

        Assert.Equal("http-sse", transport.Name);
        Assert.Equal("session-123", transport.SessionId);

        var headers = Assert.IsType<Dictionary<string, string>>(transport.Properties["headers"]);
        Assert.Equal("abc", headers["X-Test"]);
        Assert.Equal("session-123", headers[McpConstants.McpSessionIdHeaderName]);
    }

    [Fact]
    public void GetTransportInformation_WithoutAccessor_ReturnsUnknownTransport()
    {
        var transport = McpTriggerTransportHelper.GetTransportInformation(services: null);

        Assert.Equal("unknown", transport.Name);
        Assert.Null(transport.SessionId);
        Assert.Empty(transport.Properties);
    }

    [Fact]
    public void GetTransportInformation_WithNullHttpContext_ReturnsUnknownTransport()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = null
        });

        var serviceProvider = services.BuildServiceProvider();

        var transport = McpTriggerTransportHelper.GetTransportInformation(serviceProvider);

        Assert.Equal("unknown", transport.Name);
        Assert.Null(transport.SessionId);
        Assert.Empty(transport.Properties);
    }

    [Fact]
    public void GetTransportInformation_WithoutTransportNameInItems_DefaultsToHttp()
    {
        var services = new ServiceCollection();
        var httpContext = new DefaultHttpContext();

        // No transport name set in Items
        httpContext.Request.Headers["X-Custom"] = "value";

        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var serviceProvider = services.BuildServiceProvider();

        var transport = McpTriggerTransportHelper.GetTransportInformation(serviceProvider);

        Assert.Equal("http", transport.Name);
        Assert.Null(transport.SessionId);
        Assert.True(transport.Properties.ContainsKey("headers"));
    }

    [Fact]
    public void GetTransportInformation_WithoutSessionIdHeader_SessionIdIsNull()
    {
        var services = new ServiceCollection();
        var httpContext = new DefaultHttpContext();

        httpContext.Items[McpConstants.McpTransportName] = "streamable-http";
        httpContext.Request.Headers["Content-Type"] = "application/json";
        // No session ID header

        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var serviceProvider = services.BuildServiceProvider();

        var transport = McpTriggerTransportHelper.GetTransportInformation(serviceProvider);

        Assert.Equal("streamable-http", transport.Name);
        Assert.Null(transport.SessionId);

        var headers = Assert.IsType<Dictionary<string, string>>(transport.Properties["headers"]);
        Assert.Equal("application/json", headers["Content-Type"]);
    }

    [Fact]
    public void GetTransportInformation_WithMultipleHeaderValues_JoinsWithComma()
    {
        var services = new ServiceCollection();
        var httpContext = new DefaultHttpContext();

        httpContext.Items[McpConstants.McpTransportName] = "http";
        httpContext.Request.Headers.Append("Accept", "text/plain");
        httpContext.Request.Headers.Append("Accept", "application/json");

        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        var serviceProvider = services.BuildServiceProvider();

        var transport = McpTriggerTransportHelper.GetTransportInformation(serviceProvider);

        var headers = Assert.IsType<Dictionary<string, string>>(transport.Properties["headers"]);
        Assert.Equal("text/plain,application/json", headers["Accept"]);
    }
}
