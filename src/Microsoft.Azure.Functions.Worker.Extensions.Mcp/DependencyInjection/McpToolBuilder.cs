// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Builder;

public sealed class McpToolBuilder(IFunctionsWorkerApplicationBuilder builder, string toolName)
{
    public McpToolBuilder WithProperty(string name, string type, string description, bool required = false)
    {
        builder.Services.Configure<ToolOptions>(toolName, o => o.AddProperty(name, type, description, required));

        return this;
    }
}
