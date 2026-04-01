// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class StreamableHttpRequestHandlerTests
{
    private readonly Mock<IMcpInstanceIdProvider> _mockInstanceIdProvider;
    private readonly Mock<IMcpClientSessionManager> _mockClientSessionManager;
    private readonly Mock<IServiceProvider> _mockApplicationServices;
    private readonly Mock<IOptions<McpServerOptions>> _mockMcpServerOptions;
    private readonly Mock<IOptions<McpOptions>> _mockMcpOptions;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly StreamableHttpRequestHandler _handler;

    public StreamableHttpRequestHandlerTests()
    {
        _mockInstanceIdProvider = new Mock<IMcpInstanceIdProvider>();
        _mockClientSessionManager = new Mock<IMcpClientSessionManager>();
        _mockApplicationServices = new Mock<IServiceProvider>();
        _mockMcpServerOptions = new Mock<IOptions<McpServerOptions>>();
        _mockMcpOptions = new Mock<IOptions<McpOptions>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();

        _handler = new StreamableHttpRequestHandler(
            _mockInstanceIdProvider.Object,
            _mockClientSessionManager.Object,
            _mockApplicationServices.Object,
            _mockMcpServerOptions.Object,
            _mockMcpOptions.Object,
            _mockLoggerFactory.Object
        );
    }

    [Fact]
    public async Task HandleRequest_ShouldReturn405ForNonPostRequests()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;

        await _handler.HandleRequestAsync(context);

        Assert.Equal(StatusCodes.Status405MethodNotAllowed, context.Response.StatusCode);
    }

    // --- HandleDeleteRequestAsync ---

    [Fact]
    public async Task HandleRequest_Delete_Returns400_WhenSessionHeaderMissing()
    {
        _mockMcpOptions.Setup(o => o.Value).Returns(new McpOptions { EncryptClientState = false });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Delete;

        await _handler.HandleRequestAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleRequest_Delete_Returns400_WhenSessionHeaderInvalid()
    {
        _mockMcpOptions.Setup(o => o.Value).Returns(new McpOptions { EncryptClientState = false });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Delete;
        context.Request.Headers["Mcp-Session-Id"] = "not-a-valid-state";

        await _handler.HandleRequestAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleRequest_Delete_Returns204_WhenSessionHeaderValid()
    {
        _mockMcpOptions.Setup(o => o.Value).Returns(new McpOptions { EncryptClientState = false });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Delete;
        context.Request.Headers["Mcp-Session-Id"] = "client-id|instance-id";

        await _handler.HandleRequestAsync(context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleRequest_Delete_CreatesSessionEndSpan_WhenSessionHeaderValid()
    {
        _mockMcpOptions.Setup(o => o.Value).Returns(new McpOptions { EncryptClientState = false });

        var startedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = a => startedActivities.Add(a)
        };
        ActivitySource.AddActivityListener(listener);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Delete;
        context.Request.Headers["Mcp-Session-Id"] = "client-id|instance-id";

        await _handler.HandleRequestAsync(context);

        Assert.Contains(startedActivities, a => a.OperationName == SemanticConventions.Methods.SessionDelete);
    }
}
