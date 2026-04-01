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

        string type;
        string? content;
        string? structuredContent = null;

        // Determine type, content, and structured content based on the function result
        switch (functionResult)
        {
            case CallToolResult callToolResult:
                // Validate if structured content exists, ensure text content exists for backwards compatibility
                if (callToolResult.StructuredContent != null)
                {
                    var hasTextContent = callToolResult.Content?.Any(c => c is TextContentBlock) ?? false;
                    if (!hasTextContent)
                    {
                        throw new InvalidOperationException(
                            "CallToolResult contains StructuredContent but no TextContent block for backwards compatibility." +
                            " Please ensure that the CallToolResult includes a TextContent block.");
                    }
                }

                // Don't process CallToolResult - just serialize as-is
                type = Sdk.Constants.CallToolResultType;
                content = JsonSerializer.Serialize(callToolResult, McpJsonUtilities.DefaultOptions);
                structuredContent = callToolResult.StructuredContent?.ToJsonString();
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
        var type = Sdk.Constants.MultiContentResult;
        var content = JsonSerializer.Serialize(blocks, McpJsonUtilities.DefaultOptions);
        return (type, content);
    }

    private static (string Type, string Content, string? StructuredContent) ProcessDefaultResult(object functionResult)
    {
        string? structuredContent = null;
        string text;

        if (ShouldCreateStructuredContent(functionResult))
        {
            // If there is a McpContentAttribute, create structured content
            text = JsonSerializer.Serialize(functionResult, McpJsonUtilities.DefaultOptions);
            structuredContent = text;
        }
        else
        {
            // For primitives and strings, convert to text
            text = functionResult is string s ? s : JsonSerializer.Serialize(functionResult, McpJsonUtilities.DefaultOptions);
        }

        // Common for both paths: create TextContent
        var type = Sdk.Constants.TextContextResult;
        var content = JsonSerializer.Serialize(new TextContentBlock
            {
                Text = text
            }, McpJsonUtilities.DefaultOptions);

        return (type, content, structuredContent);
    }

    private static bool IsMcpToolInvocation(FunctionContext context)
    {
        return context.Items.ContainsKey(Sdk.Constants.ToolInvocationContextKey);
    }

    private static bool HasOutputBindings(FunctionContext context)
    {
        return context.FunctionDefinition.OutputBindings.Any();
    }

        /// <summary>
        /// Determines whether structured content should be created for the given object.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Returns true if the type is directly decorated with <see cref="McpContentAttribute"/>.
        /// For collections, users should create a wrapper type with the attribute, e.g.:
        /// <code>
        /// [McpContent]
        /// public class ImageList : List&lt;MyImage&gt; { }
        /// </code>
        /// </para>
        /// 
        /// <para><b>Supported Types:</b></para>
        /// <list type="bullet">
        ///   <item><c>class</c>, <c>record class</c>, <c>struct</c>, <c>record struct</c> - Fully supported</item>
        ///   <item><c>interface</c>, <c>enum</c> - Not supported (cannot be decorated with attributes in a meaningful way)</item>
        /// </list>
        /// 
        /// <para><b>Not Supported:</b></para>
        /// <list type="bullet">
        ///   <item>Inherited attribution: Only direct decoration with <see cref="McpContentAttribute"/> is recognized</item>
        ///   <item>Automatic collection element detection: Users must explicitly mark collection wrapper types</item>
        /// </list>
        /// </remarks>
        /// <param name="obj">The object to evaluate.</param>
        /// <returns>True if structured content should be created; otherwise, false.</returns>
        private static bool ShouldCreateStructuredContent(object obj)
        {
            var type = obj.GetType();
            return type.HasMcpContentAttribute();
        }
    }
