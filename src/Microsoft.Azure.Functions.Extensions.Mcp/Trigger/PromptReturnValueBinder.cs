// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Host.Bindings;
using ModelContextProtocol;
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
            // Try to deserialize as McpPromptResult wrapper from SDK middleware
            if (TryDeserializeMcpPromptResult(stringValue, out var mcpPromptResult))
            {
                var result = ProcessMcpPromptResult(mcpPromptResult);
                executionContext.SetResult(result);
                return Task.CompletedTask;
            }

            // Try to deserialize as GetPromptResult JSON directly
            if (TryDeserializeGetPromptResult(stringValue, out var promptResult))
            {
                executionContext.SetResult(promptResult);
                return Task.CompletedTask;
            }

            // Otherwise treat as a single user text message
            executionContext.SetResult(WrapTextAsPromptResult(stringValue));
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

    private static GetPromptResult ProcessMcpPromptResult(McpPromptResult mcpResult)
    {
        if (string.Equals(mcpResult.Type, McpConstants.PromptResultContentTypes.GetPromptResult, StringComparison.OrdinalIgnoreCase))
        {
            var result = JsonSerializer.Deserialize<GetPromptResult>(mcpResult.Content!, McpJsonUtilities.DefaultOptions)
                ?? throw new InvalidOperationException("Failed to deserialize GetPromptResult from McpPromptResult content.");

            return result;
        }

        if (string.Equals(mcpResult.Type, McpConstants.PromptResultContentTypes.PromptMessages, StringComparison.OrdinalIgnoreCase))
        {
            var messages = JsonSerializer.Deserialize<IList<PromptMessage>>(mcpResult.Content!, McpJsonUtilities.DefaultOptions)
                ?? throw new InvalidOperationException("Failed to deserialize PromptMessage list from McpPromptResult content.");

            return new GetPromptResult { Messages = messages };
        }

        throw new InvalidOperationException($"Unknown McpPromptResult type: '{mcpResult.Type}'.");
    }

    private static GetPromptResult WrapTextAsPromptResult(string text)
    {
        return new GetPromptResult
        {
            Messages =
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock { Text = text }
                }
            ]
        };
    }

    private static bool TryDeserializeMcpPromptResult(string jsonString, out McpPromptResult result)
    {
        result = null!;

        try
        {
            var deserialized = JsonSerializer.Deserialize<McpPromptResult>(jsonString, McpJsonSerializerOptions.DefaultOptions);

            if (deserialized?.Type is not null && deserialized.Content is not null)
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

    private static bool TryDeserializeGetPromptResult(string jsonString, out GetPromptResult result)
    {
        result = null!;

        try
        {
            var deserialized = JsonSerializer.Deserialize<GetPromptResult>(jsonString, McpJsonSerializerOptions.DefaultOptions);

            if (deserialized?.Messages is not null)
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
