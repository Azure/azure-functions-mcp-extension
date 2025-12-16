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

        // Only process results for MCP tool invocations.
        if (!IsMcpToolInvocation(context))
        {
            return;
        }

        var functionResult = context.GetInvocationResult().Value;

        // If the function returned null, we return an empty result.
        if (functionResult is null)
        {
            return;
        }

        var textBlock = new TextContentBlock
        {
            Text = functionResult is string s ? s : JsonSerializer.Serialize(functionResult)
        };

        var (type, content) = functionResult switch
        {
            ContentBlock block => (block.Type, JsonSerializer.Serialize(block, McpJsonUtilities.DefaultOptions)),
            IList<ContentBlock> blocks => (Constants.MultiContentResult, JsonSerializer.Serialize(blocks, McpJsonUtilities.DefaultOptions)),
            _ => (Constants.TextContextResult, JsonSerializer.Serialize(textBlock, McpJsonUtilities.DefaultOptions))
        };

        var mcpToolResult = new McpToolResult { Type = type, Content = content };

        context.GetInvocationResult().Value = JsonSerializer.Serialize(mcpToolResult, McpJsonContext.Default.McpToolResult);
    }

    private static bool IsMcpToolInvocation(FunctionContext context)
    {
        return context.Items.ContainsKey(Constants.ToolInvocationContextKey);
    }
}
