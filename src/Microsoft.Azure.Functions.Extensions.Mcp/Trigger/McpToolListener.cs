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
                                      ICollection<IMcpToolProperty> properties) : IListener, IMcpTool
{
    public ITriggeredFunctionExecutor Executor { get; } = executor;

    public string FunctionName { get; } = functionName;

    public string Name { get; } = toolName;

    public string? Description { get; set; } = toolDescription;

    public ICollection<IMcpToolProperty> Properties { get; set; } = properties;

    public void Dispose() { }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Cancel() { }

    public async Task<CallToolResult> RunAsync(RequestContext<CallToolRequestParams> callToolRequest, CancellationToken cancellationToken)
    {
        // Validate required properties are present in the incoming request.
        ValidateArgumentsHaveRequiredProperties(Properties, callToolRequest.Params);

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

        if (toolResult is null)
        {
            return new CallToolResult { Content = [] };
        }

        return new CallToolResult
        {
            Content =
            [
                new TextContentBlock
                {
                    Text = toolResult.ToString() ?? string.Empty,
                }
            ]
        };
    }

    internal static void ValidateArgumentsHaveRequiredProperties(ICollection<IMcpToolProperty> properties, CallToolRequestParams? callToolRequest)
    {
        if (properties is null || properties.Count == 0)
        {
            return;
        }

        var missing = new List<string>();
        var args = callToolRequest?.Arguments;

        foreach (var prop in properties)
        {
            if (!prop.IsRequired)
            {
                continue;
            }

            if (args == null
                || !args.TryGetValue(prop.PropertyName, out JsonElement value)
                || IsValueNullOrUndefined(value))
            {
                missing.Add(prop.PropertyName);
            }
        }

        if (missing.Count > 0)
        {
            // Fail early with an MCP InvalidParams error so the client sees a validation error instead of
            // the invocation proceeding to the worker with null values.
            throw new McpException($"One or more required tool properties are missing values. Please provide: {string.Join(", ", missing)}", McpErrorCode.InvalidParams);
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
