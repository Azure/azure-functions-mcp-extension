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
                (type, content, structuredContent) = ProcessCallToolResult(callToolResult);
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

    private static (string Type, string Content, string? StructuredContent) ProcessCallToolResult(CallToolResult callToolResult)
    {
        string? structuredContent = null;

        // User returned CallToolResult directly - use it as-is but ensure TextContent backup exists
        // if structured content is present (for backwards compatibility)
        if (callToolResult.StructuredContent != null)
        {
            structuredContent = callToolResult.StructuredContent.ToJsonString();

            // Ensure there's a TextContent block for backwards compatibility
            var hasTextContent = callToolResult.Content?.Any(c => c is TextContentBlock) ?? false;
            if (!hasTextContent)
            {
                throw new InvalidOperationException(
                    "CallToolResult contains StructuredContent but no TextContent block for backwards compatibility.");
            }
        }

        // Serialize the CallToolResult content
        string type;
        string content;

        if (callToolResult.Content == null || callToolResult.Content.Count == 0)
        {
            // No content blocks - create a default text block
            type = Constants.TextContextResult;
            content = JsonSerializer.Serialize(new TextContentBlock { Text = "" }, McpJsonUtilities.DefaultOptions);
        }
        else if (callToolResult.Content.Count == 1)
        {
            // Single content block
            type = callToolResult.Content[0].Type;
            content = JsonSerializer.Serialize(callToolResult.Content[0], McpJsonUtilities.DefaultOptions);
        }
        else
        {
            // Multiple content blocks
            type = Constants.MultiContentResult;
            content = JsonSerializer.Serialize(callToolResult.Content, McpJsonUtilities.DefaultOptions);
        }

        return (type, content, structuredContent);
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

        if (IsPocoInstance(functionResult))
        {
            // For POCOs (non-string, non-primitive types), serialize as structured content
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

    private static bool IsPocoInstance(object obj)
    {
        var type = obj.GetType();

        if (type == typeof(string) || !type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
        {
            return false;
        }

        return !typeof(IEnumerable).IsAssignableFrom(type)
           && type.GetConstructor(Type.EmptyTypes) is not null;
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
