// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;
using Microsoft.Azure.Functions.Extensions.Mcp.Validation;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpToolListener(ITriggeredFunctionExecutor executor,
                                      string functionName,
                                      string toolName,
                                      string? toolDescription,
                                      ToolInputSchema requestHandler,
                                      IReadOnlyDictionary<string, object?> metadata) : IListener, IMcpTool
{
    private readonly McpActivityFactory _activityFactory = new();
    private IHttpContextAccessor? _httpContextAccessor;
    private bool _httpContextAccessorResolved;

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
        // Capture transport context before creating activity
        var transportContext = ActivityHelper.CaptureCurrentContext();

        // Extract session ID and build request context with client info
        var sessionId = callToolRequest.Server?.SessionId;
        var requestContext = McpRequestTraceContext.FromHttpContextAccessor(
            GetHttpContextAccessor(callToolRequest.Services),
            sessionId);

        // Create activity for this tool execution
        using var activity = _activityFactory.CreateToolActivity(
            Name,
            callToolRequest.Params,
            transportContext,
            requestContext);

        // Start timing for metrics
        var startTimestamp = Stopwatch.GetTimestamp();
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
                activity?.SetExceptionStatus(result.Exception);
                throw result.Exception;
            }

            var toolResult = await execution.ResultTask;

            if (toolResult is CallToolResult callToolResult)
            {
                // Check for tool error (isError=true)
                if (callToolResult.IsError == true)
                {
                    errorType = SemanticConventions.Error.ToolError;
                    activity?.SetToolError();
                }

                return callToolResult;
            }

            // We did not receive a CallToolResult from the function execution,
            // return an empty result.
            return new CallToolResult { Content = [] };
        }
        catch (McpProtocolException ex)
        {
            errorType = ((int)ex.ErrorCode).ToString();
            activity?.SetJsonRpcError((int)ex.ErrorCode, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            errorType = ex.GetType().FullName;
            activity?.SetExceptionStatus(ex);
            throw;
        }
        finally
        {
            McpServerMetrics.RecordToolDuration(Stopwatch.GetElapsedTime(startTimestamp), Name, requestContext.SessionId, errorType);
        }
    }

    private IHttpContextAccessor? GetHttpContextAccessor(IServiceProvider? services)
    {
        if (!_httpContextAccessorResolved)
        {
            _httpContextAccessor = services?.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
            _httpContextAccessorResolved = true;
        }

        return _httpContextAccessor;
    }
}
