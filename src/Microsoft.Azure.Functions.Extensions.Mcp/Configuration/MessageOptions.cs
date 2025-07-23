// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp.Configuration;

/// <summary>
/// Options for configuring message handling in the Model Context Protocol (MCP) extension.
/// </summary>
public sealed class MessageOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to use the absolute URI in the endpoint MCP message.
    /// Defaults to false. Setting to true may be useful for some client implementations, but isn't recommended for all scenarios.
    /// </summary>
    public bool UseAbsoluteUriForEndpoint { get; set; } = false;
}
