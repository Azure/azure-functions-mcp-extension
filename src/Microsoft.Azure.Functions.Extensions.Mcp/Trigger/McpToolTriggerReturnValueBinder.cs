// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Bindings;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal class McpToolTriggerReturnValueBinder(CallToolExecutionContext executionContext) : IValueBinder
{
    public Type Type { get; } = typeof(object);

    public Task SetValueAsync(object value, CancellationToken cancellationToken)
    {
        var result = value is null
            ? null
            : new CallToolResult
            {
                Content =
                [
                    new TextContentBlock { Text = value.ToString() ?? string.Empty }
                ]
            };

        executionContext.SetResult(result!);
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
