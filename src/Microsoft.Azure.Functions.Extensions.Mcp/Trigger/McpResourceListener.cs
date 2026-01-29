// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        var execution = new ReadResourceExecutionContext(readResourceRequest);

        var input = new TriggeredFunctionData
        {
            TriggerValue = execution
        };

        var result = await Executor.TryExecuteAsync(input, cancellationToken);

         if (!result.Succeeded)
        {
            throw result.Exception;
        }

        var resourceResult = await execution.ResultTask;

        if (resourceResult is ReadResourceResult readResourceResult)
        {
            return readResourceResult;
        }

        return new ReadResourceResult { Contents = [] };
    }
}