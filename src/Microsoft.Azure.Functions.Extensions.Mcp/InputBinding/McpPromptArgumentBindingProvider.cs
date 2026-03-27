// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public class McpPromptArgumentBindingProvider : IBindingProvider
{
    public Task<IBinding?> TryCreateAsync(BindingProviderContext context)
    {
        var parameter = context.Parameter;
        var attribute = parameter.GetCustomAttribute<McpPromptArgumentAttribute>(inherit: false);

        return attribute == null
            ? Task.FromResult<IBinding?>(null)
            : Task.FromResult<IBinding?>(new PromptArgumentInputBinding(attribute));
    }
}
