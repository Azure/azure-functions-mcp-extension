// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests.Diagnostics;

public class McpRequestTraceContextTests
{
    [Fact]
    public void FromHttpContext_ReturnsContextWithSessionId_WhenHttpContextIsNull()
    {
        var result = McpRequestTraceContext.FromHttpContext(null, "session-123", "2024-11-05");

        Assert.Equal("session-123", result.SessionId);
        Assert.Equal("2024-11-05", result.McpProtocolVersion);
        Assert.Null(result.ClientAddress);
        Assert.Null(result.ClientPort);
        Assert.Null(result.HttpProtocolVersion);
    }

    [Fact]
    public void FromHttpContext_ExtractsClientAddress()
    {
        var httpContext = CreateHttpContext(
            remoteIpAddress: IPAddress.Parse("192.168.1.100"),
            remotePort: 54321,
            protocol: "HTTP/1.1");

        var result = McpRequestTraceContext.FromHttpContext(httpContext, "session-123");

        Assert.Equal("192.168.1.100", result.ClientAddress);
    }

    [Fact]
    public void FromHttpContext_ExtractsClientPort()
    {
        var httpContext = CreateHttpContext(
            remoteIpAddress: IPAddress.Loopback,
            remotePort: 54321,
            protocol: "HTTP/1.1");

        var result = McpRequestTraceContext.FromHttpContext(httpContext, "session-123");

        Assert.Equal(54321, result.ClientPort);
    }

    [Fact]
    public void FromHttpContext_ReturnsNullClientPort_WhenPortIsZero()
    {
        var httpContext = CreateHttpContext(
            remoteIpAddress: IPAddress.Loopback,
            remotePort: 0,
            protocol: "HTTP/1.1");

        var result = McpRequestTraceContext.FromHttpContext(httpContext, "session-123");

        Assert.Null(result.ClientPort);
    }

    [Fact]
    public void FromHttpContext_ExtractsHttpVersion_FromHttp11Protocol()
    {
        var httpContext = CreateHttpContext(
            remoteIpAddress: IPAddress.Loopback,
            remotePort: 54321,
            protocol: "HTTP/1.1");

        var result = McpRequestTraceContext.FromHttpContext(httpContext, "session-123");

        Assert.Equal("1.1", result.HttpProtocolVersion);
    }

    [Fact]
    public void FromHttpContext_ExtractsHttpVersion_FromHttp2Protocol()
    {
        var httpContext = CreateHttpContext(
            remoteIpAddress: IPAddress.Loopback,
            remotePort: 54321,
            protocol: "HTTP/2");

        var result = McpRequestTraceContext.FromHttpContext(httpContext, "session-123");

        Assert.Equal("2", result.HttpProtocolVersion);
    }

    [Fact]
    public void FromHttpContext_ReturnsNullHttpVersion_WhenProtocolIsEmpty()
    {
        var httpContext = CreateHttpContext(
            remoteIpAddress: IPAddress.Loopback,
            remotePort: 54321,
            protocol: "");

        var result = McpRequestTraceContext.FromHttpContext(httpContext, "session-123");

        Assert.Null(result.HttpProtocolVersion);
    }

    [Fact]
    public void FromHttpContext_HandlesIpv6Address()
    {
        var httpContext = CreateHttpContext(
            remoteIpAddress: IPAddress.Parse("::1"),
            remotePort: 54321,
            protocol: "HTTP/1.1");

        var result = McpRequestTraceContext.FromHttpContext(httpContext, "session-123");

        Assert.Equal("::1", result.ClientAddress);
    }

    [Fact]
    public void FromHttpContextAccessor_ReturnsContextWithSessionId_WhenAccessorIsNull()
    {
        var result = McpRequestTraceContext.FromHttpContextAccessor(null, "session-123", "2024-11-05");

        Assert.Equal("session-123", result.SessionId);
        Assert.Equal("2024-11-05", result.McpProtocolVersion);
        Assert.Null(result.ClientAddress);
    }

    [Fact]
    public void FromHttpContextAccessor_ReturnsContextWithSessionId_WhenHttpContextIsNull()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var result = McpRequestTraceContext.FromHttpContextAccessor(accessor.Object, "session-123");

        Assert.Equal("session-123", result.SessionId);
        Assert.Null(result.ClientAddress);
    }

    [Fact]
    public void FromHttpContextAccessor_ExtractsContext_WhenHttpContextIsAvailable()
    {
        var httpContext = CreateHttpContext(
            remoteIpAddress: IPAddress.Parse("10.0.0.1"),
            remotePort: 12345,
            protocol: "HTTP/2");

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        var result = McpRequestTraceContext.FromHttpContextAccessor(accessor.Object, "session-123", "2024-11-05");

        Assert.Equal("session-123", result.SessionId);
        Assert.Equal("10.0.0.1", result.ClientAddress);
        Assert.Equal(12345, result.ClientPort);
        Assert.Equal("2", result.HttpProtocolVersion);
        Assert.Equal("2024-11-05", result.McpProtocolVersion);
    }

    [Fact]
    public void DefaultContext_HasAllNullProperties()
    {
        var result = default(McpRequestTraceContext);

        Assert.Null(result.SessionId);
        Assert.Null(result.ClientAddress);
        Assert.Null(result.ClientPort);
        Assert.Null(result.HttpProtocolVersion);
        Assert.Null(result.McpProtocolVersion);
    }

    private static HttpContext CreateHttpContext(IPAddress? remoteIpAddress, int remotePort, string protocol)
    {
        var connectionMock = new Mock<ConnectionInfo>();
        connectionMock.Setup(c => c.RemoteIpAddress).Returns(remoteIpAddress);
        connectionMock.Setup(c => c.RemotePort).Returns(remotePort);

        var requestMock = new Mock<HttpRequest>();
        requestMock.Setup(r => r.Protocol).Returns(protocol);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(c => c.Connection).Returns(connectionMock.Object);
        httpContextMock.Setup(c => c.Request).Returns(requestMock.Object);

        return httpContextMock.Object;
    }
}
