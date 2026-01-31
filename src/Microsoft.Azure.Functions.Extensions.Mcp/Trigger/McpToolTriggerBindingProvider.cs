// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpToolTriggerBindingProvider(IToolRegistry toolRegistry, McpMetrics mcpMetrics, ILoggerFactory loggerFactory) : ITriggerBindingProvider
{
    public Task<ITriggerBinding?> TryCreateAsync(TriggerBindingProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var parameterInfo = context.Parameter;
        var attribute = parameterInfo.GetCustomAttribute<McpToolTriggerAttribute>(false);
        if (attribute == null)
        {
            return Task.FromResult<ITriggerBinding?>(null);
        }

        var binding = new McpToolTriggerBinding(context.Parameter, toolRegistry, attribute, mcpMetrics, loggerFactory);

        return Task.FromResult<ITriggerBinding?>(binding);
    }
}
