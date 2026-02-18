// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpResourceListener(ITriggeredFunctionExecutor executor,
                                          string functionName,
                                          string resourceUri,
                                          string resourceName,
                                          string? resourceDescription,
                                          string? resourceMimeType,
                                          long? resourceSize,
                                          IReadOnlyDictionary<string, object?> metadata) : IListener, IMcpResource
{
    private readonly McpActivityFactory _activityFactory = new();
    private IHttpContextAccessor? _httpContextAccessor;
    private bool _httpContextAccessorResolved;

    public ITriggeredFunctionExecutor Executor { get; } = executor;

    public string FunctionName { get; } = functionName;

    public string Uri { get; } = resourceUri;

    public string Name { get; set; } = resourceName;

    public string? Description { get; set; } = resourceDescription;

    public string? MimeType { get; set; } = resourceMimeType;

    public long? Size { get; set; } = resourceSize;

    public IReadOnlyDictionary<string, object?> Metadata { get; set; } = metadata;

    public void Dispose() { }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Cancel() { }

    public async Task<ReadResourceResult> ReadAsync(RequestContext<ReadResourceRequestParams> readResourceRequest, CancellationToken cancellationToken)
    {
        // Capture transport context before creating activity
        var transportContext = ActivityHelper.CaptureCurrentContext();

        // Extract session ID and build request context with client info
        var sessionId = readResourceRequest.Server?.SessionId;
        var requestContext = McpRequestTraceContext.FromHttpContextAccessor(
            GetHttpContextAccessor(readResourceRequest.Services),
            sessionId);

        // Create activity for this resource read
        using var activity = _activityFactory.CreateResourceActivity(
            Uri,
            readResourceRequest.Params,
            transportContext,
            requestContext);

        // Start timing for metrics
        var startTimestamp = Stopwatch.GetTimestamp();
        string? errorType = null;

        try
        {
            var execution = new ReadResourceExecutionContext(readResourceRequest);

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

            var resourceResult = await execution.ResultTask;

            if (resourceResult is ReadResourceResult readResourceResult)
            {
                return readResourceResult;
            }

            return new ReadResourceResult { Contents = [] };
        }
        catch (Exception ex)
        {
            errorType = ex.GetType().FullName;
            activity?.SetExceptionStatus(ex);
            throw;
        }
        finally
        {
            McpServerMetrics.RecordResourceDuration(Stopwatch.GetElapsedTime(startTimestamp), Uri, requestContext.SessionId, errorType);
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
