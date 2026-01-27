// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Executors;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpResourceListenerTests
{
    private static RequestContext<ReadResourceRequestParams> CreateRequest(string uri = "test://resource/1")
    {
        var server = new Mock<McpServer>().Object;
        var parameters = new ReadResourceRequestParams { Uri = uri };

        return new RequestContext<ReadResourceRequestParams>(server, new JsonRpcRequest() { Method = RequestMethods.ResourcesRead })
        {
            Params = parameters
        };
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var metadata = new Dictionary<string, object?> { { "key1", "value1" } };

        var listener = new McpResourceListener(
            executor,
            "MyFunction",
            "test://resource/1",
            "TestResource",
            "A test resource",
            "text/plain",
            1024,
            metadata);

        Assert.Equal("MyFunction", listener.FunctionName);
        Assert.Equal("test://resource/1", listener.Uri);
        Assert.Equal("TestResource", listener.Name);
        Assert.Equal("A test resource", listener.Description);
        Assert.Equal("text/plain", listener.MimeType);
        Assert.Equal(1024, listener.Size);
        Assert.Same(metadata, listener.Metadata);
    }

    [Fact]
    public async Task StartAsync_Completes()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var listener = new McpResourceListener(
            executor,
            "MyFunction",
            "test://resource/1",
            "TestResource",
            null,
            null,
            null,
            new Dictionary<string, object?>());

        await listener.StartAsync(CancellationToken.None);
        // Should complete without throwing
    }

    [Fact]
    public async Task StopAsync_Completes()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var listener = new McpResourceListener(
            executor,
            "MyFunction",
            "test://resource/1",
            "TestResource",
            null,
            null,
            null,
            new Dictionary<string, object?>());

        await listener.StopAsync(CancellationToken.None);
        // Should complete without throwing
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var listener = new McpResourceListener(
            executor,
            "MyFunction",
            "test://resource/1",
            "TestResource",
            null,
            null,
            null,
            new Dictionary<string, object?>());

        listener.Dispose();
        // Should complete without throwing
    }

    [Fact]
    public void Cancel_DoesNotThrow()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var listener = new McpResourceListener(
            executor,
            "MyFunction",
            "test://resource/1",
            "TestResource",
            null,
            null,
            null,
            new Dictionary<string, object?>());

        listener.Cancel();
        // Should complete without throwing
    }
}
