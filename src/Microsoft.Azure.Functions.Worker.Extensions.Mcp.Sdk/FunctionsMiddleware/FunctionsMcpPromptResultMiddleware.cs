// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk;
using Microsoft.Azure.Functions.Worker.Middleware;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

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

        // If there are output bindings, don't wrap the result
        if (HasOutputBindings(context))
        {
            return;
        }

        string type;
        string? content;

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
                // For strings and other types, convert to a text prompt message
                string text = functionResult is string s
                    ? s
                    : JsonSerializer.Serialize(functionResult, McpJsonUtilities.DefaultOptions);

                type = Constants.GetPromptResultType;
                var result = new GetPromptResult
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
                content = JsonSerializer.Serialize(result, McpJsonUtilities.DefaultOptions);
                break;
        }

        var mcpPromptResult = new McpPromptResult
        {
            Type = type,
            Content = content
        };

        _resultAccessor.SetResult(context, JsonSerializer.Serialize(mcpPromptResult, McpJsonContext.Default.McpPromptResult));
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
