// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;
using Microsoft.Azure.Functions.Extensions.Mcp.Validation;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;
using static Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics.TraceConstants;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpToolListener(ITriggeredFunctionExecutor executor,
                                      string functionName,
                                      string toolName,
                                      string? toolDescription,
                                      ToolInputSchema requestHandler,
                                      IReadOnlyDictionary<string, object?> metadata,
                                      McpMetrics? metrics = null) : IListener, IMcpTool
{
    public ITriggeredFunctionExecutor Executor { get; } = executor;

    public string FunctionName { get; } = functionName;

    public string Name { get; } = toolName;

    public string? Description { get; set; } = toolDescription;

    public IReadOnlyDictionary<string, object?> Metadata { get; } = metadata;

    public ToolInputSchema ToolInputSchema { get; } = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));

    public void Dispose()
    {
        // Dispose the validator if it implements IDisposable (e.g., JsonSchemaToolInputSchema)
        if (ToolInputSchema is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Cancel() { }

    public async Task<CallToolResult> RunAsync(RequestContext<CallToolRequestParams> callToolRequest, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        string? errorType = null;

        try
        {
            // Validate required properties are present in the incoming request.
            ToolInputSchema.Validate(callToolRequest.Params);

            var execution = new CallToolExecutionContext(callToolRequest);

            var input = new TriggeredFunctionData
            {
                TriggerValue = execution
            };

            var result = await Executor.TryExecuteAsync(input, cancellationToken);

            if (!result.Succeeded)
            {
                errorType = result.Exception?.GetType().FullName;
                throw result.Exception!;
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
        catch (Exception ex)
        {
            errorType ??= ex.GetType().FullName;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            metrics?.RecordToolCallDuration(
                durationSeconds: stopwatch.Elapsed.TotalSeconds,
                toolName: Name,
                sessionId: callToolRequest.Server.SessionId,
                errorType: errorType);
        }
    }
}
