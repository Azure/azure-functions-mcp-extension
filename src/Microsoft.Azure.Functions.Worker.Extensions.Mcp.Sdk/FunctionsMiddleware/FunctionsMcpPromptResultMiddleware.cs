// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk;
using Microsoft.Azure.Functions.Worker.Middleware;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Wraps the worker's prompt-function return value into an <see cref="McpPromptResult"/>
/// envelope (Type + serialized Content) for the host binder to deserialize.
/// </summary>
/// <remarks>
/// Mirrors <see cref="FunctionsMcpToolResultMiddleware"/>. The envelope's <c>Type</c>
/// discriminator lets the host accept any successfully deserialized payload — including
/// empty <c>GetPromptResult</c> values — without shape-sniffing the JSON.
/// </remarks>
internal class FunctionsMcpPromptResultMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IFunctionResultAccessor _resultAccessor;

    public FunctionsMcpPromptResultMiddleware(IFunctionResultAccessor? resultAccessor = null)
    {
        _resultAccessor = resultAccessor ?? new DefaultFunctionResultAccessor();
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);

        await next(context);

        if (!IsMcpPromptInvocation(context))
        {
            return;
        }

        var functionResult = _resultAccessor.GetResult(context);

        if (functionResult is null)
        {
            return;
        }

        // If there are output bindings, don't wrap the result - let it flow to the output binding.
        if (HasOutputBindings(context))
        {
            return;
        }

        string type;
        string content;

        switch (functionResult)
        {
            case GetPromptResult getPromptResult:
                type = Constants.GetPromptResultType;
                content = JsonSerializer.Serialize(getPromptResult, McpJsonUtilities.DefaultOptions);
                break;

            case PromptMessage promptMessage:
                type = Constants.PromptMessagesType;
                content = JsonSerializer.Serialize(new[] { promptMessage }, McpJsonUtilities.DefaultOptions);
                break;

            case IList<PromptMessage> messages:
                type = Constants.PromptMessagesType;
                content = JsonSerializer.Serialize(messages, McpJsonUtilities.DefaultOptions);
                break;

            default:
                // Strings and other arbitrary types collapse into a single User text message.
                string text = functionResult is string s
                    ? s
                    : JsonSerializer.Serialize(functionResult, McpJsonUtilities.DefaultOptions);

                type = Constants.GetPromptResultType;
                content = JsonSerializer.Serialize(
                    new GetPromptResult
                    {
                        Messages =
                        [
                            new PromptMessage
                            {
                                Role = Role.User,
                                Content = new TextContentBlock { Text = text }
                            }
                        ]
                    },
                    McpJsonUtilities.DefaultOptions);
                break;
        }

        var envelope = new McpPromptResult
        {
            Type = type,
            Content = content
        };

        _resultAccessor.SetResult(context, JsonSerializer.Serialize(envelope, McpJsonContext.Default.McpPromptResult));
    }

    private static bool IsMcpPromptInvocation(FunctionContext context)
    {
        return context.Items.ContainsKey(Constants.PromptInvocationContextKey);
    }

    private static bool HasOutputBindings(FunctionContext context)
    {
        return context.FunctionDefinition.OutputBindings.Any();
    }
}
