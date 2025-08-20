// Copyright (c) Mi// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Represents an MCP client session
/// </summary>
/// <typeparam name="TTransport">The transport type</typeparam>
internal interface IMcpClientSession<out TTransport> : IAsyncDisposable
    where TTransport : IMcpExtensionTransport
{
    /// <summary>
    /// Gets the session transport
    /// </summary>
    TTransport Transport { get; }

    /// <summary>
    /// Gets or sets the MCP server instance
    /// </summary>
    IMcpServer? Server { get; set; }

    /// <summary>
    /// Handles a message asynchronously
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// Default implementation of MCP client session
/// </summary>
/// <typeparam name="TTransport">The transport type</typeparam>
internal sealed class DefaultMcpClientSession<TTransport> : IMcpClientSession<TTransport>
    where TTransport : IMcpExtensionTransport
{
    /// <summary>
    /// Initializes a new instance of the DefaultMcpClientSession class
    /// </summary>
    /// <param name="transport">The transport</param>
    public DefaultMcpClientSession(TTransport transport)
    {
        Transport = transport;
    }

    /// <summary>
    /// Gets the session transport
    /// </summary>
    public TTransport Transport { get; }

    /// <summary>
    /// Gets or sets the MCP server instance
    /// </summary>
    public IMcpServer? Server { get; set; }

    /// <summary>
    /// Handles a message asynchronously
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken)
    {
        return Transport.HandleMessageAsync(message, cancellationToken);
    }

    /// <summary>
    /// Disposes of the session resources
    /// </summary>
    /// <returns>A task representing the asynchronous disposal</returns>
    public ValueTask DisposeAsync()
    {
        return Transport.DisposeAsync();
    }
}

/// <summary>
/// Interface for managing MCP client sessions
/// </summary>
internal interface IMcpClientSessionManager
{
    /// <summary>
    /// Creates a new session asynchronously
    /// </summary>
    /// <typeparam name="TTransport">The transport type</typeparam>
    /// <param name="sessionId">The session ID</param>
    /// <param name="instanceId">The instance ID</param>
    /// <param name="transport">The transport</param>
    /// <returns>A task representing the asynchronous operation that returns the created session</returns>
    ValueTask<IMcpClientSession<TTransport>> CreateSessionAsync<TTransport>(string sessionId, string instanceId, TTransport transport)
        where TTransport : IMcpExtensionTransport;
}

/// <summary>
/// Default implementation of MCP client session manager
/// </summary>
internal sealed class DefaultMcpClientSessionManager : IMcpClientSessionManager
{
    /// <summary>
    /// Creates a new session asynchronously
    /// </summary>
    /// <typeparam name="TTransport">The transport type</typeparam>
    /// <param name="sessionId">The session ID</param>
    /// <param name="instanceId">The instance ID</param>
    /// <param name="transport">The transport</param>
    /// <returns>A task representing the asynchronous operation that returns the created session</returns>
    public ValueTask<IMcpClientSession<TTransport>> CreateSessionAsync<TTransport>(string sessionId, string instanceId, TTransport transport)
        where TTransport : IMcpExtensionTransport
    {
        var session = new DefaultMcpClientSession<TTransport>(transport);
        return ValueTask.FromResult<IMcpClientSession<TTransport>>(session);
    }
}
