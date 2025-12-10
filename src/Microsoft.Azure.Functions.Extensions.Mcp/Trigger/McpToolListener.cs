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

    public JsonElement? InputSchema 
    { 
        get => _inputSchema; 
        set
        {
            if (value.HasValue && !McpInputSchemaJsonUtilities.IsValidMcpToolSchema(value.Value))
            {
                throw new ArgumentException(
                    "The specified document is not a valid MCP tool input JSON schema.",
                    nameof(InputSchema));
            }
            _inputSchema = value;
        }
    }

    private JsonElement? _inputSchema = inputSchema;

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

    /// <summary>
    /// Validates that all required arguments are present in the tool call request.
    /// Uses InputSchema if provided (worker mode), otherwise validates against Properties (extension mode).
    /// </summary>
    internal static void ValidateArgumentsHaveRequiredProperties(
        ICollection<IMcpToolProperty> properties,
        CallToolRequestParams? callToolRequest,
        JsonElement? inputSchema)
    {
        var missing = new List<string>();
        var args = callToolRequest?.Arguments;
        var requiredProperties = new List<string>();

        // Use InputSchema if provided and it has required properties
        if (inputSchema.HasValue)
        {
            // Extract required properties from the schema using utility
            requiredProperties.AddRange(McpInputSchemaJsonUtilities.GetRequiredProperties(inputSchema.Value));
        }
        else
        {
            // Fall back to Properties approach
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
