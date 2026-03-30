// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Host.Bindings;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class PromptReturnValueBinder(GetPromptExecutionContext executionContext) : IValueBinder
{
    public Type Type { get; } = typeof(object);

    public Task SetValueAsync(object value, CancellationToken cancellationToken)
    {
        if (value is null)
        {
            executionContext.SetResult(new GetPromptResult { Messages = [] });
            return Task.CompletedTask;
        }

        if (value is string stringValue)
        {
            // Try to deserialize as GetPromptResult JSON from worker
            if (TryDeserializeGetPromptResult(stringValue, out var promptResult))
            {
                executionContext.SetResult(promptResult);
                return Task.CompletedTask;
            }

            // Otherwise treat as a single user text message
            var result = new GetPromptResult
            {
                Messages =
                [
                    new PromptMessage
                    {
                        Role = Role.User,
                        Content = new TextContentBlock { Text = stringValue }
                    }
                ]
            };

            executionContext.SetResult(result);
            return Task.CompletedTask;
        }

        throw new InvalidOperationException(
            $"Unsupported return type for prompt: {value.GetType().Name}. Expected string.");
    }

    public Task<object> GetValueAsync()
    {
        throw new NotSupportedException();
    }

    public string ToInvokeString()
    {
        return string.Empty;
    }

    private static bool TryDeserializeGetPromptResult(string jsonString, out GetPromptResult result)
    {
        result = null!;

        try
        {
            var deserialized = JsonSerializer.Deserialize<GetPromptResult>(jsonString, McpJsonSerializerOptions.DefaultOptions);

            if (deserialized?.Messages is { Count: > 0 } || deserialized?.Description is not null)
            {
                result = deserialized;
                return true;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
