// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public static class CallToolExecutionContextHelper
{
    internal static CallToolExecutionContext CreateExecutionContext(string toolName = "MyTool",
                                                                   IReadOnlyDictionary<string, JsonElement>? args = null,
                                                                   string? sessionId = "session-123",
                                                                   Implementation? clientInfo = null,
                                                                   IHttpContextAccessor? httpContextAccessor = null)
    {
        args ??= new Dictionary<string, JsonElement>
        {
            ["arg1"] = JsonDocument.Parse("\"value1\"").RootElement,
            ["num"] = JsonDocument.Parse("1").RootElement
        };

        clientInfo ??= new Implementation { Name = "client", Version = "1.0" };

        var requestParams = new CallToolRequestParams
        {
            Name = toolName,
            Arguments = (IDictionary<string, JsonElement>)args
        };

        var services = new ServiceCollection();

        if (httpContextAccessor is not null)
        {
            services.AddSingleton(httpContextAccessor);
        }

        var mockServer = new Mock<McpServer>();
        mockServer.Setup(s=> s.SessionId).Returns(sessionId);
        mockServer.Setup(s => s.ClientInfo).Returns(clientInfo);

        RequestContext<CallToolRequestParams> requestContext = new(mockServer.Object, new JsonRpcRequest() { Method = RequestMethods.ToolsCall})
        {
            Params = requestParams,
            Services = services.BuildServiceProvider()
        };

        CallToolExecutionContext executionContext = new(requestContext);

        return executionContext;
    }
}
