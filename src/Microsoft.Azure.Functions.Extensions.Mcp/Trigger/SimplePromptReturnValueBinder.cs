// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Bindings;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Return value binder used when the worker is not wrapping prompt results in the
/// SDK <see cref="McpPromptResult"/> envelope (e.g. in-process workers, or isolated
/// workers without the SDK middleware). Treats the raw return value as text and
/// wraps it in a single-message <see cref="GetPromptResult"/>.
/// </summary>
internal sealed class SimplePromptReturnValueBinder(GetPromptExecutionContext executionContext) : IValueBinder
{
    public Type Type { get; } = typeof(object);

    public Task SetValueAsync(object value, CancellationToken cancellationToken)
    {
        if (value is null)
        {
            executionContext.SetResult(new GetPromptResult { Messages = [] });
            return Task.CompletedTask;
        }

        var text = value.ToString() ?? string.Empty;

        executionContext.SetResult(new GetPromptResult
        {
            Messages =
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock { Text = text }
                }
            ]
        });

        return Task.CompletedTask;
    }

    public Task<object> GetValueAsync() => throw new NotSupportedException();

    public string ToInvokeString() => string.Empty;
}
