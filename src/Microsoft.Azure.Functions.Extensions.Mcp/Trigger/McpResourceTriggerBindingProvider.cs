// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpResourceTriggerBindingProvider(IResourceRegistry resourceRegistry, ILoggerFactory loggerFactory) : ITriggerBindingProvider
{
    public Task<ITriggerBinding?> TryCreateAsync(TriggerBindingProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var parameter = context.Parameter;
        var attribute = parameter.GetCustomAttribute<McpResourceTriggerAttribute>(inherit: false);

        if (attribute is null)
        {
            return Task.FromResult<ITriggerBinding?>(null);
        }

        return Task.FromResult<ITriggerBinding?>(new McpResourceTriggerBinding(parameter, resourceRegistry, attribute, loggerFactory));
    }
}
