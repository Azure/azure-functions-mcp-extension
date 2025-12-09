// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpToolListener(ITriggeredFunctionExecutor executor,
                                      string functionName,
                                      string toolName,
                                      string? toolDescription,
                                      ICollection<IMcpToolProperty> properties,
                                      JsonElement? inputSchema) : IListener, IMcpTool
{
    public ITriggeredFunctionExecutor Executor { get; } = executor;

    public string FunctionName { get; } = functionName;

    public string Name { get; } = toolName;

    public string? Description { get; set; } = toolDescription;

    public ICollection<IMcpToolProperty> Properties { get; set; } = properties;

    public JsonElement InputSchema { get; set; } = inputSchema ?? McpInputSchemaJsonUtilities.DefaultMcpToolSchema;

    public void Dispose() { }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Cancel() { }

    public async Task<CallToolResult> RunAsync(RequestContext<CallToolRequestParams> callToolRequest, CancellationToken cancellationToken)
    {
        // Validate required properties are present in the incoming request.
        ValidateArgumentsHaveRequiredProperties(Properties, callToolRequest.Params, InputSchema);

        var execution = new CallToolExecutionContext(callToolRequest);

        var input = new TriggeredFunctionData
        {
            TriggerValue = execution
        };

        var result = await Executor.TryExecuteAsync(input, cancellationToken);

        if (!result.Succeeded)
        {
            throw result.Exception;
        }

        var toolResult = await execution.ResultTask;

        if (toolResult is CallToolResult callToolResult)
        {
            return callToolResult;
        }

        // We did not receive a CallToolResult from the function execution,
        // return an empty result.
        return new CallToolResult { Content = [] };
    }

    internal static void ValidateArgumentsHaveRequiredProperties(ICollection<IMcpToolProperty> properties, CallToolRequestParams? callToolRequest)
    {
        ValidateArgumentsHaveRequiredProperties(properties, callToolRequest, McpInputSchemaJsonUtilities.DefaultMcpToolSchema);
    }

    internal static void ValidateArgumentsHaveRequiredProperties(ICollection<IMcpToolProperty> properties, CallToolRequestParams? callToolRequest, JsonElement inputSchema)
    {
        var missing = new List<string>();
        var args = callToolRequest?.Arguments;
        var requiredProperties = new List<string>();

        // Use InputSchema if it's not the default empty schema, otherwise fall back to Properties
        bool hasCustomInputSchema = !McpInputSchemaJsonUtilities.IsDefaultSchema(inputSchema);
        
        if (hasCustomInputSchema)
        {
            // Extract required properties from the schema
            requiredProperties.AddRange(McpInputSchemaJsonUtilities.GetRequiredProperties(inputSchema));
        }
        else
        {
            // Fall back to the original method using Properties
            if (properties is null || properties.Count == 0)
            {
                return;
            }

            requiredProperties.AddRange(properties
                .Where(p => p.IsRequired)
                .Select(p => p.PropertyName));
        }

        // If no required properties, return early
        if (requiredProperties.Count == 0)
        {
            return;
        }

        // Check for missing required properties
        foreach (var propertyName in requiredProperties)
        {
            if (args == null
                || !args.TryGetValue(propertyName, out JsonElement value)
                || IsValueNullOrUndefined(value))
            {
                missing.Add(propertyName);
            }
        }

        if (missing.Count > 0)
        {
            // Fail early with an MCP InvalidParams error so the client sees a validation error instead of
            // the invocation proceeding to the worker with null values.
            throw new McpProtocolException($"One or more required tool properties are missing values. Please provide: {string.Join(", ", missing)}", McpErrorCode.InvalidParams);
        }
    }

    private static bool IsValueNullOrUndefined(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null => true,
            JsonValueKind.Undefined => true,
            _ => false
        };
    }
}
