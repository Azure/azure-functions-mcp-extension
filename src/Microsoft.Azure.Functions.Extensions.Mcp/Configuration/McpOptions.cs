// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Hosting;
using Newtonsoft.Json.Linq;
using System.ComponentModel;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Configuration;

/// <summary>
/// Options for configuring the Model Context Protocol (MCP) extension in Azure Functions.
/// </summary>
public sealed class McpOptions : IOptionsFormatter
{
    private MessageOptions _messageOptions = new();

    /// <summary>
    /// Gets or sets the name of the server that is returned to the client in the initialization response.
    /// </summary>
    public string ServerName { get; set; } = "Azure Functions MCP server";

    /// <summary>
    /// Gets or sets the server version that is returned to the client in the initialization response.
    /// </summary>
    public string ServerVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the instructions returned to the client in the initialization response.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to encrypt the client state in the endpoint URL.
    /// Defaults to true. Setting to false may be useful for debugging and test scenarios, but isn't recommended for production.
    /// </summary>
    public bool EncryptClientState { get; set; } = true;


    /// <summary>
    /// Gets or sets the options used to configure MCP message behavior.
    /// </summary>
    public MessageOptions MessageOptions
    {
        get => _messageOptions;
        set => _messageOptions = value
                                 ?? throw new ArgumentNullException(nameof(value),"Message options cannot be null.");
    }

    /// <inheritdoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    string IOptionsFormatter.Format()
    {
        JObject options = new JObject
            {
                { nameof(ServerName), ServerName },
                { nameof(ServerVersion), ServerVersion },
                { nameof(Instructions), Instructions },
                { nameof(EncryptClientState), EncryptClientState },
                { nameof(MessageOptions) + "." + nameof(MessageOptions.UseAbsoluteUriForEndpoint), MessageOptions.UseAbsoluteUriForEndpoint }
            };

        return options.ToString(Newtonsoft.Json.Formatting.Indented);
    }
}