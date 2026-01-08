// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk;
using Microsoft.Azure.Functions.Worker.Middleware;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using System.Collections;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal class FunctionsMcpToolResultMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IFunctionResultAccessor _resultAccessor;

    public FunctionsMcpToolResultMiddleware(IFunctionResultAccessor? resultAccessor = null)
    {
        _resultAccessor = resultAccessor ?? new DefaultFunctionResultAccessor();
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);

        await next(context);

        // Only process results for MCP tool invocations.
        if (!IsMcpToolInvocation(context))
        {
            return;
        }

        var functionResult = _resultAccessor.GetResult(context);

        // If the function returned null, we return an empty result.
        if (functionResult is null)
        {
            return;
        }

        // If there are output bindings, don't wrap the result - let it flow to the output binding
        // The host-side extension will handle creating the MCP result from the raw value
        if (HasOutputBindings(context))
        {
            return;
        }

        string type;
        string? content;
        string? structuredContent = null;

        // Determine type, content, and structured content based on the function result
        switch (functionResult)
        {
            case CallToolResult callToolResult:
                // Don't process CallToolResult - just serialize as-is
                type = Constants.CallToolResultType;
                content = JsonSerializer.Serialize(callToolResult, McpJsonUtilities.DefaultOptions);
                break;

            case ContentBlock block:
                (type, content) = ProcessContentBlock(block);
                break;

            case IList<ContentBlock> blocks:
                (type, content) = ProcessContentBlockList(blocks);
                break;

            default:
                (type, content, structuredContent) = ProcessDefaultResult(functionResult);
                break;
        }

        var mcpToolResult = new McpToolResult
        {
            Type = type,
            Content = content,
            StructuredContent = structuredContent
        };

        _resultAccessor.SetResult(context, JsonSerializer.Serialize(mcpToolResult, McpJsonContext.Default.McpToolResult));
    }

    private static (string Type, string Content) ProcessContentBlock(ContentBlock block)
    {
        var type = block.Type;
        var content = JsonSerializer.Serialize(block, McpJsonUtilities.DefaultOptions);
        return (type, content);
    }

    private static (string Type, string Content) ProcessContentBlockList(IList<ContentBlock> blocks)
    {
        var type = Constants.MultiContentResult;
        var content = JsonSerializer.Serialize(blocks, McpJsonUtilities.DefaultOptions);
        return (type, content);
    }

    private static (string Type, string Content, string? StructuredContent) ProcessDefaultResult(object functionResult)
    {
        string? structuredContent = null;
        string text;

        if (ShouldCreateStructuredContent(functionResult))
        {
            // If there is a McpResultAttribute, create structured content
            text = JsonSerializer.Serialize(functionResult, McpJsonUtilities.DefaultOptions);
            structuredContent = text;
        }
        else
        {
            // For primitives and strings, convert to text
            text = functionResult is string s ? s : JsonSerializer.Serialize(functionResult, McpJsonUtilities.DefaultOptions);
        }

        // Common for both paths: create TextContent
        var type = Constants.TextContextResult;
        var content = JsonSerializer.Serialize(new TextContentBlock
            {
                Text = text
            }, McpJsonUtilities.DefaultOptions);

        return (type, content, structuredContent);
    }

    private static bool ShouldCreateStructuredContent(object obj)
    {
        var type = obj.GetType();

        // Check if the type is decorated with McpResultAttribute
        if (type.GetCustomAttributes(typeof(McpResultAttribute), inherit: false).Length > 0)
        {
            return true;
        }

        // Check if it's an array or enumerable of types with McpResultAttribute
        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            // Get the element type
            Type? elementType = null;

            if (type.IsArray)
            {
                elementType = type.GetElementType();
            }
            else if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();
                if (genericArgs.Length > 0)
                {
                    elementType = genericArgs[0];
                }
            }

            // Check if the element type has McpResultAttribute
            if (elementType != null && 
                elementType.GetCustomAttributes(typeof(McpResultAttribute), inherit: false).Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMcpToolInvocation(FunctionContext context)
    {
        return context.Items.ContainsKey(Constants.ToolInvocationContextKey);
    }

    private static bool HasOutputBindings(FunctionContext context)
    {
        return context.FunctionDefinition.OutputBindings.Any();
    }
}
