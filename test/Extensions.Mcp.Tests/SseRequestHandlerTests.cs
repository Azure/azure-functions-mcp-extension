// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Moq;
using Microsoft.Azure.WebJobs.Extensions.Mcp;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class SseRequestHandlerTests
{
    private readonly IMcpInstanceIdProvider _instanceIdProvider;

    public SseRequestHandlerTests()
    {
        var idProviderMock = new Mock<IMcpInstanceIdProvider>();
        idProviderMock.Setup(p => p.InstanceId)
            .Returns("instance");
        _instanceIdProvider = idProviderMock.Object;
    }

    private static IOptions<McpOptions> CreateOptions(bool useAbsoluteUri)
    {
        return Options.Create(new McpOptions
        {
            EncryptClientState = false,
            MessageOptions = new MessageOptions { UseAbsoluteUriForEndpoint = useAbsoluteUri }
        });
    }

    [Fact]
    public void WriteEndpoint_ReturnsRelativeUri_WhenUseAbsoluteUriForEndpointIsFalse()
    {
        var options = CreateOptions(false);
        var serverOptions = Options.Create(new McpServerOptions());
        var handler = new SseRequestHandler(null!, _instanceIdProvider, options, serverOptions, NullLoggerFactory.Instance);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/api/sse",
                PathBase = ""
            }
        };

        var endpoint = handler.GetMessageEndpoint("client", context);

        Assert.StartsWith("message?azmcpcs=", endpoint);
        Assert.DoesNotContain("http://", endpoint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WriteEndpoint_ReturnsAbsoluteUri_WhenUseAbsoluteUriForEndpointIsTrue()
    {
        var options = CreateOptions(true);
        var serverOptions = Options.Create(new McpServerOptions());
        var handler = new SseRequestHandler(null!, _instanceIdProvider, options, serverOptions, NullLoggerFactory.Instance);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Scheme = "https",
                Host = new HostString("example.com"),
                Path = "/runtime/webhooks/sse",
                PathBase = ""
            }
        };

        var endpoint = handler.GetMessageEndpoint("client", context);
        Assert.StartsWith("https://example.com/runtime/webhooks/message?azmcpcs=", endpoint);
    }

    [Fact]
    public void WriteEndpoint_AppendsFunctionKey_WhenPresent()
    {
        var options = CreateOptions(false);
        var serverOptions = Options.Create(new McpServerOptions());
        var handler = new SseRequestHandler(null!, _instanceIdProvider, options, serverOptions, NullLoggerFactory.Instance);
        var context = new DefaultHttpContext
        {
            Request =
            {
                QueryString = new QueryString("?code=abc123"),
                Query = new QueryCollection(new Dictionary<string, StringValues> { { "code", new StringValues("abc123") } })
            }
        };

        var endpoint = handler.GetMessageEndpoint("client", context);

        Assert.Contains("code=abc123", endpoint);
    }

    [Theory]
    [InlineData("/api/sse", true)]
    [InlineData("/api/Sse", true)]
    [InlineData("/api/message", true)]
    [InlineData("/api/other", false)]
    [InlineData("/api/sse/", true)]
    [InlineData("/api/message/", true)]
    [InlineData("/api/other/", false)]
    public void IsSseRequest_ValidatesCorrectly(string path, bool expectedResult)
    {
        var options = CreateOptions(false);
        var serverOptions = Options.Create(new McpServerOptions());
        var handler = new SseRequestHandler(null!, _instanceIdProvider, options, serverOptions, NullLoggerFactory.Instance);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = path
            }
        };

        var result = handler.IsSseRequest(context);

        Assert.Equal(expectedResult, result);
    }
}
