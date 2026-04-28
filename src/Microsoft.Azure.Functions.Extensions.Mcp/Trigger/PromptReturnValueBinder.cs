// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Host.Bindings;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Return value binder used when the worker SDK middleware wraps prompt results
/// in an <see cref="McpPromptResult"/> envelope. Unwraps the envelope and
/// deserializes the inner payload based on the envelope's <c>Type</c> discriminator,
/// so any successfully deserialized payload — including empty results — round-trips
/// without shape-sniffing.
/// </summary>
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

        if (value is not string jsonString)
        {
            throw new ArgumentException("Expected JSON string.", nameof(value));
        }

        McpPromptResult envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<McpPromptResult>(jsonString, McpJsonSerializerOptions.DefaultOptions)
                ?? throw new InvalidOperationException("The function return value could not be deserialized to a valid McpPromptResult.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("The function return value could not be deserialized to a valid McpPromptResult.", ex);
        }

        if (envelope.Content is not { } content)
        {
            throw new InvalidOperationException("McpPromptResult.Content was null; cannot process prompt result.");
        }

        try
        {
            executionContext.SetResult(ProcessMcpPromptResult(envelope.Type, content));
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize content for type '{envelope.Type}'.", ex);
        }

        return Task.CompletedTask;
    }

    public Task<object> GetValueAsync() => throw new NotSupportedException();

    public string ToInvokeString() => string.Empty;

    private static GetPromptResult ProcessMcpPromptResult(string type, string content)
    {
        if (string.Equals(type, McpConstants.PromptResultContentTypes.GetPromptResult, StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Deserialize<GetPromptResult>(content, McpJsonUtilities.DefaultOptions)
                ?? throw new InvalidOperationException("Failed to deserialize GetPromptResult from McpPromptResult content.");
        }

        if (string.Equals(type, McpConstants.PromptResultContentTypes.PromptMessages, StringComparison.OrdinalIgnoreCase))
        {
            var messages = JsonSerializer.Deserialize<IList<PromptMessage>>(content, McpJsonUtilities.DefaultOptions)
                ?? throw new InvalidOperationException("Failed to deserialize PromptMessage list from McpPromptResult content.");

            return new GetPromptResult { Messages = messages };
        }

        throw new InvalidOperationException($"Unknown McpPromptResult type: '{type}'.");
    }
}

