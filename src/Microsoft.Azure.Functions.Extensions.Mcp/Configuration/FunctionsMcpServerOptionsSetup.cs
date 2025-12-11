// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Configuration;

internal class FunctionsMcpServerOptionsSetup(IOptions<McpOptions> extensionOptions) : IConfigureOptions<McpServerOptions>
{
    public void Configure(McpServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var mcpOptions = extensionOptions.Value;

        options.ServerInfo = new Implementation
        {
            Name = mcpOptions.ServerName,
            Version = mcpOptions.ServerVersion
        };

        options.ServerInstructions = mcpOptions.Instructions;

        options.Capabilities = new ServerCapabilities
        {
            Tools = new ToolsCapability(),
            Resources = new ResourcesCapability()
        };
    }
}
