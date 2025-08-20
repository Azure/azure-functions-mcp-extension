// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

public sealed class McpExtensionStartup : WorkerExtensionStartup
{
   public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
   {
       // Auto-enable MCP with default settings to provide a seamless experience
       // Enable MCP with default settings (StreamableHttp enabled, no endpoint suppression)
       applicationBuilder.EnableMcp();
   }
}
