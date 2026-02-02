// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
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
                            "CallToolResult contains StructuredContent but no TextContent block for backwards compatibility.");
                    }
                }

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

    /// <summary>
    /// Determines whether structured content should be created for the given object.
    /// </summary>
    /// <remarks>
    /// <para><b>Type Resolution Rules (evaluated in order):</b></para>
    /// <list type="number">
    ///   <item>
    ///     <term>Direct Attribution</term>
    ///     <description>If the type is decorated with <see cref="McpResultAttribute"/>, returns true.</description>
    ///   </item>
    ///   <item>
    ///     <term>Collection Element Attribution</term>
    ///     <description>If the type is a collection and the element type has <see cref="McpResultAttribute"/>, returns true.
    ///     Nested collections are recursively checked.</description>
    ///   </item>
    ///   <item>
    ///     <term>No Attribution</term>
    ///     <description>Otherwise, returns false (text content only).</description>
    ///   </item>
    /// </list>
    /// 
    /// <para><b>Supported Types:</b></para>
    /// <list type="bullet">
    ///   <item><c>class</c>, <c>record class</c>, <c>struct</c>, <c>record struct</c> - Fully supported</item>
    ///   <item><c>interface</c>, <c>enum</c> - Not supported (cannot be decorated with attributes in a meaningful way)</item>
    /// </list>
    /// 
    /// <para><b>Collection Handling:</b></para>
    /// <list type="bullet">
    ///   <item>Arrays: Element type is checked recursively</item>
    ///   <item>Generic collections (List, IEnumerable, etc.): First type argument is checked recursively</item>
    ///   <item>Dictionaries: Not supported</item>
    ///   <item>Nested collections: Recursively unwrapped until a non-collection element type is found</item>
    /// </list>
    /// 
    /// <para><b>Not Supported:</b></para>
    /// <list type="bullet">
    ///   <item>Inherited attribution: Only direct decoration with <see cref="McpResultAttribute"/> is recognized</item>
    ///   <item>Dictionary types: Any type implementing <c>IEnumerable&lt;KeyValuePair&lt;,&gt;&gt;</c></item>
    /// </list>
    /// </remarks>
    /// <param name="obj">The object to evaluate.</param>
    /// <returns>True if structured content should be created; otherwise, false.</returns>
    private static bool ShouldCreateStructuredContent(object obj)
    {
        var type = obj.GetType();
        return HasMcpResultAttributeRecursive(type);
    }

    private static bool HasMcpResultAttributeRecursive(Type type)
    {
        // Check if the type itself is decorated with McpResultAttribute (no inheritance)
        if (HasMcpResultAttribute(type))
        {
            return true;
        }

        // Skip dictionary types entirely
        if (IsDictionaryType(type))
        {
            return false;
        }

        // Check if it's a collection type
        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            var elementType = GetElementType(type);

            if (elementType != null)
            {
                // Recursively check element type (handles nested collections)
                if (HasMcpResultAttributeRecursive(elementType))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasMcpResultAttribute(Type type)
    {
        // Only check for direct attribution, not inherited
        return type.GetCustomAttributes(typeof(McpResultAttribute), inherit: false).Length > 0;
    }

    private static bool IsDictionaryType(Type type)
    {
        // Check if the type implements IEnumerable<KeyValuePair<,>>
        // All dictionary types (Dictionary<,>, IDictionary<,>, IReadOnlyDictionary<,>, etc.) implement this
        return type.GetInterfaces()
            .Any(i => i.IsGenericType &&
                      i.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                      i.GetGenericArguments()[0].IsGenericType &&
                      i.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(KeyValuePair<,>));
    }

    private static Type? GetElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (!type.IsGenericType)
        {
            return null;
        }

        var genericArgs = type.GetGenericArguments();

        // Handle generic collections - check the first type argument
        return genericArgs.Length > 0 ? genericArgs[0] : null;
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
