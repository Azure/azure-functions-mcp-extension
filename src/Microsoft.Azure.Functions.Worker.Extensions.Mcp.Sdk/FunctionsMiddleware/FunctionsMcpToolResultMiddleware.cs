// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk;
using Microsoft.Azure.Functions.Worker.Middleware;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

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

        var (type, content) = functionResult switch
        {
            case ContentBlock block:
                type = block.Type;
                content = JsonSerializer.Serialize(block, McpJsonUtilities.DefaultOptions);
                break;

            case IList<ContentBlock> blocks:
                type = Constants.MultiContentResult;
                content = JsonSerializer.Serialize(blocks, McpJsonUtilities.DefaultOptions);
                break;

            default:
                // For other types, check if they have structured content properties
                structuredContent = TryExtractStructuredContentFromObject(functionResult);
                
                type = Constants.TextContextResult;
                content = JsonSerializer.Serialize(new TextContentBlock
                {
                    Text = functionResult is string s ? s : JsonSerializer.Serialize(functionResult)
                }, McpJsonUtilities.DefaultOptions);
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

    private static string? TryExtractStructuredContentFromObject(object obj)
    {
        // Try to extract structured content from objects with StructuredContent properties
        var objType = obj.GetType();
        var structuredContentProperty = objType.GetProperty("StructuredContent");
        
        if (structuredContentProperty != null)
        {
            var structuredContentValue = structuredContentProperty.GetValue(obj);
            if (structuredContentValue != null)
            {
                // Handle various types that might contain structured content
                // Not sure if we need to handle this?
                if (structuredContentValue is JsonElement jsonElement)
                {
                    return jsonElement.GetRawText();
                }
                else if (structuredContentValue is string stringValue)
                {
                    return stringValue;
                }
                else
                {
                    // Serialize the object as JSON
                    return JsonSerializer.Serialize(structuredContentValue);
                }
            }
        }
        
        return null;
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
