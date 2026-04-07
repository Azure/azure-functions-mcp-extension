// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public static class GetPromptExecutionContextHelper
{
    internal static GetPromptExecutionContext CreateExecutionContext(
        string promptName = "TestPrompt",
        IDictionary<string, JsonElement>? args = null,
        string? sessionId = "session-123",
        Implementation? clientInfo = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        clientInfo ??= new Implementation { Name = "client", Version = "1.0" };

        var requestParams = new GetPromptRequestParams
        {
            Name = promptName,
            Arguments = args
        };

        var services = new ServiceCollection();

        if (httpContextAccessor is not null)
        {
            services.AddSingleton(httpContextAccessor);
        }

        var mockServer = new Mock<McpServer>();
        mockServer.Setup(s => s.SessionId).Returns(sessionId);
        mockServer.Setup(s => s.ClientInfo).Returns(clientInfo);

        var requestContext = new RequestContext<GetPromptRequestParams>(
            mockServer.Object,
            new JsonRpcRequest() { Method = RequestMethods.PromptsGet },
            requestParams)
        {
            Services = services.BuildServiceProvider()
        };

        return new GetPromptExecutionContext(requestContext);
    }
}
