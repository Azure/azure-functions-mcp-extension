using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpClientSessionManagerTests
{
    private readonly Mock<ILogger<McpClientSessionManager>> _loggerMock;
    private readonly McpClientSessionManager _sessionManager;

    public McpClientSessionManagerTests()
    {
        _loggerMock = new Mock<ILogger<McpClientSessionManager>>();
        _sessionManager = new McpClientSessionManager(_loggerMock.Object);
    }

    [Fact]
    public async Task CreateSession_AddsSessionSuccessfully()
    {
        var transportMock = new Mock<IMcpExtensionTransport>();
        var session = await _sessionManager.CreateSessionAsync("client1", "instance1", transportMock.Object);

        Assert.NotNull(session);
        Assert.Equal("client1", session.ClientId);
        Assert.Equal("instance1", session.InstanceId);
    }

    [Fact]
    public async Task CreateSession_ThrowsException_WhenClientIdAlreadyExists()
    {
        var transportMock = new Mock<IMcpExtensionTransport>();
        await _sessionManager.CreateSessionAsync("client1", "instance1", transportMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sessionManager.CreateSessionAsync("client1", "instance2", transportMock.Object));
    }

    [Fact]
    public async Task DisposeAsync_RemovesSessionSuccessfully()
    {
        var transportMock = new Mock<IMcpExtensionTransport>();
        var serverMock = new Mock<IMcpServer>();

        var session = await _sessionManager.CreateSessionAsync("client1", "instance1", transportMock.Object);
        session.Server = serverMock.Object;

        await session.DisposeAsync();

        var getResult = await _sessionManager.TryGetSessionAsync("client1");
        Assert.False(getResult.Succeeded);
    }

    [Fact]
    public async Task TryGetSession_ReturnsTrue_WhenSessionExists()
    {
        var transportMock = new Mock<IMcpExtensionTransport>();
        await _sessionManager.CreateSessionAsync("client1", "instance1", transportMock.Object);

        var getResult = await _sessionManager.TryGetSessionAsync("client1");

        Assert.True(getResult.Succeeded);
        Assert.NotNull(getResult.Session);
    }

    [Fact]
    public async Task TryGetSession_ReturnsFalse_WhenSessionDoesNotExist()
    {
        var getResult = await _sessionManager.TryGetSessionAsync("nonexistent");

        Assert.False(getResult.Succeeded);
        Assert.Null(getResult.Session);
    }

    [Fact]
    public async Task DisposeAsync_CleansUpResources()
    {
        var transportMock = new Mock<IMcpExtensionTransport>();
        var serverMock = new Mock<IMcpServer>();

        var session = await _sessionManager.CreateSessionAsync("client1", "instance1", transportMock.Object);
        session.Server = serverMock.Object;

        await session.DisposeAsync();

        serverMock.Verify(s => s.DisposeAsync(), Times.Once);
        transportMock.Verify(t => t.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task StartPingAsync_StartsPingSuccessfully()
    {
        var transportMock = new Mock<IMcpExtensionTransport>();
        var serverMock = new Mock<IMcpServer>();

        var session = await _sessionManager.CreateSessionAsync("client1", "instance1", transportMock.Object);
        session.Server = serverMock.Object;

        var cancellationToken = new CancellationTokenSource().Token;
        await session.StartPingAsync(cancellationToken);

        Assert.NotNull(session);
    }

    [Fact]
    public async Task StopPingAsync_StopsPingSuccessfully()
    {
        var transportMock = new Mock<IMcpExtensionTransport>();
        var serverMock = new Mock<IMcpServer>();

        var session = await _sessionManager.CreateSessionAsync("client1", "instance1", transportMock.Object);
        session.Server = serverMock.Object;

        await session.StartPingAsync(CancellationToken.None);
        await session.StopPingAsync();

        Assert.NotNull(session);
    }
}
