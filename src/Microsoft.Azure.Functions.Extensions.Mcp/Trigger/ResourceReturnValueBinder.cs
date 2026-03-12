// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class ResourceReturnValueBinder(
    ReadResourceExecutionContext executionContext,
    McpResourceTriggerAttribute resourceAttribute,
    ILogger<ResourceReturnValueBinder> logger) : IValueBinder
{
    public Type Type { get; } = typeof(object);
    private readonly ILogger<ResourceReturnValueBinder> _logger = logger;

    public Task SetValueAsync(object value, CancellationToken cancellationToken)
    {
        if (value is null)
        {
            executionContext.SetResult(null!);
            return Task.CompletedTask;
        }

        executionContext.SetResult(ResourceReturnValueHelper.CreateReadResourceResult(value, resourceAttribute, _logger));
        return Task.CompletedTask;
    }

    public Task<object> GetValueAsync()
    {
        throw new NotSupportedException();
    }

    public string ToInvokeString()
    {
        return string.Empty;
    }
}
