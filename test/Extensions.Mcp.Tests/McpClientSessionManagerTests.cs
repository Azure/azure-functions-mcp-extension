using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Extensions.Mcp;
using ModelContextProtocol.Protocol.Messages;
using ModelContextProtocol.Server;
using Moq;
using Xunit;
using System.Threading;
using System.Threading.Tasks;

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
    public void CreateSession_AddsSessionSuccessfully()
    {
        var transportMock = new Mock<ITransportWithMessageHandling>();
        var session = _sessionManager.CreateSession("client1", "instance1", transportMock.Object);

        Assert.NotNull(session);
        Assert.Equal("client1", session.ClientId);
        Assert.Equal("instance1", session.InstanceId);
    }

    [Fact]
    public void CreateSession_ThrowsException_WhenClientIdAlreadyExists()
    {
        var transportMock = new Mock<ITransportWithMessageHandling>();
        _sessionManager.CreateSession("client1", "instance1", transportMock.Object);

        Assert.Throws<InvalidOperationException>(() =>
            _sessionManager.CreateSession("client1", "instance2", transportMock.Object));
    }

    [Fact]
    public async Task DisposeAsync_RemovesSessionSuccessfully()
    {
        var transportMock = new Mock<ITransportWithMessageHandling>();
        var serverMock = new Mock<IMcpServer>();

        var session = _sessionManager.CreateSession("client1", "instance1", transportMock.Object);
        session.Server = serverMock.Object;

        await session.DisposeAsync();

        Assert.False(_sessionManager.TryGetSession("client1", out _));
    }

    [Fact]
    public void TryGetSession_ReturnsTrue_WhenSessionExists()
    {
        var transportMock = new Mock<ITransportWithMessageHandling>();
        _sessionManager.CreateSession("client1", "instance1", transportMock.Object);

        var result = _sessionManager.TryGetSession("client1", out var session);

        Assert.True(result);
        Assert.NotNull(session);
    }

    [Fact]
    public void TryGetSession_ReturnsFalse_WhenSessionDoesNotExist()
    {
        var result = _sessionManager.TryGetSession("nonexistent", out var session);

        Assert.False(result);
        Assert.Null(session);
    }

    [Fact]
    public async Task DisposeAsync_CleansUpResources()
    {
        var transportMock = new Mock<ITransportWithMessageHandling>();
        var serverMock = new Mock<IMcpServer>();

        var session = _sessionManager.CreateSession("client1", "instance1", transportMock.Object);
        session.Server = serverMock.Object;

        await session.DisposeAsync();

        serverMock.Verify(s => s.DisposeAsync(), Times.Once);
        transportMock.Verify(t => t.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task StartPingAsync_StartsPingSuccessfully()
    {
        var transportMock = new Mock<ITransportWithMessageHandling>();
        var serverMock = new Mock<IMcpServer>();

        var session = _sessionManager.CreateSession("client1", "instance1", transportMock.Object);
        session.Server = serverMock.Object;

        var cancellationToken = new CancellationTokenSource().Token;
        await session.StartPingAsync(cancellationToken);

        Assert.NotNull(session);
    }

    [Fact]
    public async Task StopPingAsync_StopsPingSuccessfully()
    {
        var transportMock = new Mock<ITransportWithMessageHandling>();
        var serverMock = new Mock<IMcpServer>();

        var session = _sessionManager.CreateSession("client1", "instance1", transportMock.Object);
        session.Server = serverMock.Object;

        await session.StartPingAsync(CancellationToken.None);
        await session.StopPingAsync();

        Assert.NotNull(session);
    }
}
