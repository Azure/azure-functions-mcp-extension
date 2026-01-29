// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public static class ReadResourceExecutionContextHelper
{
    internal static ReadResourceExecutionContext CreateExecutionContext(
        string uri = "test://resource/1",
        string? sessionId = "session-123",
        Implementation? clientInfo = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        clientInfo ??= new Implementation { Name = "client", Version = "1.0" };

        var requestParams = new ReadResourceRequestParams
        {
            Uri = uri
        };

        var services = new ServiceCollection();

        if (httpContextAccessor is not null)
        {
            services.AddSingleton(httpContextAccessor);
        }

        var mockServer = new Mock<McpServer>();
        mockServer.Setup(s => s.SessionId).Returns(sessionId);
        mockServer.Setup(s => s.ClientInfo).Returns(clientInfo);

        var requestContext = new RequestContext<ReadResourceRequestParams>(
            mockServer.Object,
            new JsonRpcRequest() { Method = RequestMethods.ResourcesRead })
        {
            Params = requestParams,
            Services = services.BuildServiceProvider()
        };

        var executionContext = new ReadResourceExecutionContext(requestContext);

        return executionContext;
    }
}
