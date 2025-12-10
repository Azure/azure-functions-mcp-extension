// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Middleware;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal class FunctionsMcpToolResultMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);

        await next(context);

        // Only process results for MCP tool trigger functions
        if (!IsMcpToolTrigger(context))
        {
            return;
        }

        var functionResult = context.GetInvocationResult().Value;

        // If the function returned null, we return an empty result.
        if (functionResult is null)
        {
            return;
        }

        var (type, content) = functionResult switch
        {
            ContentBlock block => (block.Type, JsonSerializer.Serialize(block, McpJsonUtilities.DefaultOptions)),
            IList<ContentBlock> blocks => ("multi_content_result", JsonSerializer.Serialize(blocks, McpJsonUtilities.DefaultOptions)),
            _ => ("text", BuildTextContentJson(functionResult))
        };

        var mcpToolResult = new McpToolResult { Type = type, Content = content };

        context.GetInvocationResult().Value = JsonSerializer.Serialize(mcpToolResult);
    }

    private static string BuildTextContentJson(object functionResult)
    {
        string textValue;
        if (functionResult is string resultString)
        {
            textValue = resultString;
        }
        else
        {
            textValue = JsonSerializer.Serialize(functionResult);
        }

        var contnet = new
        {
            type = "text",
            text = textValue
        };

        return JsonSerializer.Serialize(contnet);
    }

    private static bool IsMcpToolTrigger(FunctionContext context)
    {
        const string McpToolTriggerBindingType = "mcpToolTrigger";

        return context.FunctionDefinition.InputBindings.Values
            .Any(b => b.Type.Equals(McpToolTriggerBindingType, StringComparison.OrdinalIgnoreCase));
    }
}
