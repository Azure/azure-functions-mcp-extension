// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

[assembly: WorkerExtensionStartup(typeof(McpExtensionStartup))]

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

public sealed class McpExtensionStartup : WorkerExtensionStartup
{
    public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
    {
        applicationBuilder.ConfigureMcpExtension();
    }
}
