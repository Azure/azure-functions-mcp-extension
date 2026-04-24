// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk;
using Microsoft.Azure.Functions.Worker.Middleware;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Normalizes the worker's prompt-function return value into a serialized
/// <see cref="GetPromptResult"/> JSON string for the host binder to consume.
/// </summary>
/// <remarks>
/// Unlike <see cref="FunctionsMcpToolResultMiddleware"/>, no envelope is needed:
/// every supported prompt return type collapses without information loss into
/// the MCP protocol's own <see cref="GetPromptResult"/> shape, which is also
/// the natural cross-language wire contract.
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

        GetPromptResult promptResult = functionResult switch
        {
            GetPromptResult result => result,
            PromptMessage message => new GetPromptResult { Messages = [message] },
            IList<PromptMessage> messages => new GetPromptResult { Messages = messages },
            string text => new GetPromptResult
            {
                Messages = [new PromptMessage { Role = Role.User, Content = new TextContentBlock { Text = text } }]
            },
            _ => new GetPromptResult
            {
                Messages =
                [
                    new PromptMessage
                    {
                        Role = Role.User,
                        Content = new TextContentBlock
                        {
                            Text = JsonSerializer.Serialize(functionResult, McpJsonUtilities.DefaultOptions)
                        }
                    }
                ]
            }
        };

        _resultAccessor.SetResult(context, JsonSerializer.Serialize(promptResult, McpJsonUtilities.DefaultOptions));
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
