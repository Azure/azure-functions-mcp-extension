// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Configuration options for MCP extension
/// </summary>
public sealed class McpOptions
{
    /// <summary>
    /// Gets or sets whether to encrypt client state
    /// </summary>
    public bool EncryptClientState { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable StreamableHttp transport. Defaults to true since SSE is deprecated.
    /// </summary>
    public bool EnableStreamableHttp { get; set; } = true;

    /// <summary>
    /// Gets or sets the message options
    /// </summary>
    public MessageOptions MessageOptions { get; set; } = new();
}

/// <summary>
/// Configuration options for message handling
/// </summary>
public sealed class MessageOptions
{
    /// <summary>
    /// Gets or sets whether to use absolute URI for message endpoints
    /// </summary>
    public bool UseAbsoluteUriForEndpoint { get; set; } = false;
}
