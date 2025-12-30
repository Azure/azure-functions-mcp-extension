// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.RegularExpressions;
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

        // Check if the function has [McpOutput] attribute
        bool hasMcpOutputAttribute = HasMcpOutputAttribute(context);
        string? structuredContent = null;
        string type;
        string content;

        // Handle different return types
        switch (functionResult)
        {
            case ContentBlock block:
                type = block.Type;
                content = JsonSerializer.Serialize(block, McpJsonUtilities.DefaultOptions);
                
                // If McpOutput is present, also serialize as structured content
                if (hasMcpOutputAttribute)
                {
                    structuredContent = JsonSerializer.Serialize(functionResult);
                }
                break;

            case IList<ContentBlock> blocks:
                type = Constants.MultiContentResult;
                content = JsonSerializer.Serialize(blocks, McpJsonUtilities.DefaultOptions);
                
                // If McpOutput is present, also serialize as structured content
                if (hasMcpOutputAttribute)
                {
                    structuredContent = JsonSerializer.Serialize(functionResult);
                }
                break;

            default:
                // For other types (POCOs, CallToolResult, etc.)
                if (hasMcpOutputAttribute)
                {
                    // Serialize the entire result as structured content
                    structuredContent = JsonSerializer.Serialize(functionResult);
                    
                    // For backwards compatibility, also return the serialized JSON in a TextContent block
                    type = Constants.TextContextResult;
                    content = JsonSerializer.Serialize(new TextContentBlock
                    {
                        Text = structuredContent
                    }, McpJsonUtilities.DefaultOptions);
                }
                else
                {
                    // Standard text content for non-McpOutput functions
                    type = Constants.TextContextResult;
                    content = JsonSerializer.Serialize(new TextContentBlock
                    {
                        Text = functionResult is string s ? s : JsonSerializer.Serialize(functionResult)
                    }, McpJsonUtilities.DefaultOptions);
                }
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

    private static readonly Regex _entryPointRegex = new Regex(@"^(?<typename>.+)\.(?<methodname>[^\.]+)$", RegexOptions.Compiled);
    private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";
    private const string FunctionsWorkerDirectoryKey = "AZUREFUNCTIONS_WORKER_DIRECTORY";
    private const string McpOutputAttributeName = "Microsoft.Azure.Functions.Worker.Extensions.Mcp.McpOutputAttribute";

    private static bool HasMcpOutputAttribute(FunctionContext context)
    {
        try
        {
            var entryPoint = context.FunctionDefinition.EntryPoint;
            if (string.IsNullOrWhiteSpace(entryPoint))
            {
                return false;
            }

            var match = _entryPointRegex.Match(entryPoint);
            if (!match.Success)
            {
                return false;
            }

            var typeName = match.Groups["typename"].Value;
            var methodName = match.Groups["methodname"].Value;

            var scriptRoot = Environment.GetEnvironmentVariable(FunctionsApplicationDirectoryKey)
                            ?? Environment.GetEnvironmentVariable(FunctionsWorkerDirectoryKey);

            if (string.IsNullOrWhiteSpace(scriptRoot))
            {
                return false;
            }

            var scriptFile = Path.Combine(scriptRoot, context.FunctionDefinition.PathToAssembly ?? string.Empty);
            var assemblyPath = Path.GetFullPath(scriptFile);
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
            var type = assembly.GetType(typeName);

            if (type is null)
            {
                return false;
            }

            var method = type.GetMethod(methodName);
            if (method is null)
            {
                return false;
            }

            // Check if the method has McpOutputAttribute
            return method.GetCustomAttributes()
                .Any(attr => attr.GetType().FullName == McpOutputAttributeName);
        }
        catch
        {
            // If reflection fails, assume no attribute
            return false;
        }
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
