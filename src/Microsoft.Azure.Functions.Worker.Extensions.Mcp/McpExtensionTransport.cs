// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Base class for MCP extension transports
/// </summary>
/// <typeparam name="TTransport">The underlying transport type</typeparam>
internal abstract class McpExtensionTransport<TTransport> : IMcpExtensionTransport where TTransport : class
{
    /// <summary>
    /// Initializes a new instance of the McpExtensionTransport class
    /// </summary>
    /// <param name="transport">The underlying transport</param>
    protected McpExtensionTransport(TTransport transport)
    {
        Transport = transport;
    }

    /// <summary>
    /// Gets the underlying transport
    /// </summary>
    protected TTransport Transport { get; }

    /// <summary>
    /// Gets or sets the session context
    /// </summary>
    public SessionContext? SessionContext { get; set; }

    /// <summary>
    /// Gets or sets whether this is a stateless transport
    /// </summary>
    public bool IsStateless { get; set; }

    /// <summary>
    /// Gets or sets the session ID
    /// </summary>
    public abstract string? SessionId { get; set; }

    /// <summary>
    /// Handles a message asynchronously
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public abstract Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Disposes of the transport resources
    /// </summary>
    /// <returns>A task representing the asynchronous disposal</returns>
    public virtual ValueTask DisposeAsync()
    {
        if (Transport is IDisposable disposable)
        {
            disposable.Dispose();
        }
        else if (Transport is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }
        
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Interface for MCP extension transports
/// </summary>
internal interface IMcpExtensionTransport : IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the session context
    /// </summary>
    SessionContext? SessionContext { get; set; }

    /// <summary>
    /// Gets or sets whether this is a stateless transport
    /// </summary>
    bool IsStateless { get; set; }

    /// <summary>
    /// Gets or sets the session ID
    /// </summary>
    string? SessionId { get; set; }

    /// <summary>
    /// Handles a message asynchronously
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken);
}
