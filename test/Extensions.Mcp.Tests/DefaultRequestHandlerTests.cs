// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class DefaultRequestHandlerTests
{
    private readonly Mock<IStreamableHttpRequestHandler> _streamableHttpRequestHandlerMock;
    private readonly Mock<ISseRequestHandler> _sseRequestHandlerMock;    
    private readonly DefaultRequestHandler _defaultRequestHandler;

    public DefaultRequestHandlerTests()
    {
        _streamableHttpRequestHandlerMock = new Mock<IStreamableHttpRequestHandler>();
        _sseRequestHandlerMock = new Mock<ISseRequestHandler>();
        
        _defaultRequestHandler = new DefaultRequestHandler(
            _streamableHttpRequestHandlerMock.Object,
            _sseRequestHandlerMock.Object);
    }

    [Fact]
    public async Task HandleRequest_CallsSseRequestHandler_WhenIsSseRequestIsTrue()
    {
        var context = new DefaultHttpContext();
        _sseRequestHandlerMock.Setup(handler => handler.IsLegacySseRequest(context)).Returns(true);

        await _defaultRequestHandler.HandleRequest(context);

        _sseRequestHandlerMock.Verify(handler => handler.HandleRequest(context), Times.Once);
        _streamableHttpRequestHandlerMock.Verify(handler => handler.HandleRequestAsync(context), Times.Never);
    }

    [Fact]
    public async Task HandleRequest_CallsStreamableHttpRequestHandler_WhenIsSseRequestIsFalse()
    {
        var context = new DefaultHttpContext();
        _sseRequestHandlerMock.Setup(handler => handler.IsLegacySseRequest(context)).Returns(false);

        await _defaultRequestHandler.HandleRequest(context);

        _streamableHttpRequestHandlerMock.Verify(handler => handler.HandleRequestAsync(context), Times.Once);
        _sseRequestHandlerMock.Verify(handler => handler.HandleRequest(context), Times.Never);
    }
}
