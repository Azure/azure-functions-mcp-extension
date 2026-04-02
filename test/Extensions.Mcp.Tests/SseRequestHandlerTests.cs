// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Azure.Functions.Extensions.Mcp.Backplane;
using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;
using Microsoft.Azure.WebJobs.Extensions.Mcp;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class SseRequestHandlerTests
{
    private readonly IMcpInstanceIdProvider _instanceIdProvider;
    private readonly IMcpClientSessionManager _clientSessionManager;
    private readonly IMcpBackplaneService _backplaneService;

    public SseRequestHandlerTests()
    {
        var idProviderMock = new Mock<IMcpInstanceIdProvider>();
        idProviderMock.Setup(p => p.InstanceId)
            .Returns("instance");
        _instanceIdProvider = idProviderMock.Object;

        var clientSessionManagerMock = new Mock<IMcpClientSessionManager>();
        _clientSessionManager = clientSessionManagerMock.Object;

        var backplaneServiceMock = new Mock<IMcpBackplaneService>();
        _backplaneService = backplaneServiceMock.Object;
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
        var handler = new SseRequestHandler(_instanceIdProvider, _clientSessionManager, _backplaneService, options, serverOptions, NullLoggerFactory.Instance);
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
        var handler = new SseRequestHandler(_instanceIdProvider, _clientSessionManager, _backplaneService, options, serverOptions, NullLoggerFactory.Instance);
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
        var handler = new SseRequestHandler(_instanceIdProvider, _clientSessionManager, _backplaneService, options, serverOptions, NullLoggerFactory.Instance);
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

    [Fact]
    public async Task HandleMessageRequestAsync_Initialize_CreatesSessionSpan()
    {
        var startedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = a => startedActivities.Add(a)
        };
        ActivitySource.AddActivityListener(listener);

        var clientId = "test-client";
        var instanceId = "instance";
        var clientState = $"{clientId}|{instanceId}";

        var mockSession = new Mock<IMcpClientSession>();
        mockSession.Setup(s => s.HandleMessageAsync(It.IsAny<JsonRpcMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sessionManagerMock = new Mock<IMcpClientSessionManager>();
        sessionManagerMock
            .Setup(m => m.TryGetSessionAsync(clientId))
            .Returns(new ValueTask<GetSessionResult>(GetSessionResult.Success(mockSession.Object)));

        var options = Options.Create(new McpOptions { EncryptClientState = false });
        var serverOptions = Options.Create(new McpServerOptions());
        var handler = new SseRequestHandler(
            _instanceIdProvider,
            sessionManagerMock.Object,
            _backplaneService,
            options,
            serverOptions,
            NullLoggerFactory.Instance);

        // initialize: must have both id and method per JsonRpcMessageConverter
        var json = "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"initialize\",\"params\":{}}";
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection(
            new Dictionary<string, StringValues> { { "azmcpcs", new StringValues(clientState) } });
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        context.Request.ContentType = "application/json";
        context.Response.Body = Stream.Null;

        await handler.HandleMessageRequestAsync(context, new McpOptions { EncryptClientState = false });

        Assert.Contains(startedActivities, a => a.OperationName == SemanticConventions.Methods.Initialize);
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
        var handler = new SseRequestHandler(_instanceIdProvider, _clientSessionManager, _backplaneService, options, serverOptions, NullLoggerFactory.Instance);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = path
            }
        };

        var result = handler.IsLegacySseRequest(context);

        Assert.Equal(expectedResult, result);
    }
}
