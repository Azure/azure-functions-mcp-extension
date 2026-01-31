// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
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
                                          IReadOnlyDictionary<string, object?> metadata,
                                          McpMetrics? metrics = null) : IListener, IMcpResource
{
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
        var stopwatch = Stopwatch.StartNew();
        string? errorType = null;

        // Add mime type to current trace activity if available
        if (!string.IsNullOrEmpty(MimeType))
        {
            Activity.Current?.SetTag(TraceConstants.McpAttributes.ResourceMimeType, MimeType);
        }

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
                throw result.Exception!;
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
            errorType ??= ex.GetType().FullName;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            metrics?.RecordResourceReadDuration(
                durationSeconds: stopwatch.Elapsed.TotalSeconds,
                resourceUri: Uri,
                mimeType: MimeType,
                sessionId: readResourceRequest.Server.SessionId,
                errorType: errorType);
        }
    }
}
