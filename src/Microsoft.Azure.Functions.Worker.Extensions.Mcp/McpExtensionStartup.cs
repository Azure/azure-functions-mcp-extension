// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

public sealed class McpExtensionStartup : WorkerExtensionStartup
{
   public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
   {
       applicationBuilder.Services.Decorate<IFunctionMetadataProvider, McpFunctionMetadataProvider>();
   }
}
