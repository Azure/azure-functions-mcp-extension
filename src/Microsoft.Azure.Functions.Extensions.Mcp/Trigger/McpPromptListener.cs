// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class McpPromptListener(
    ITriggeredFunctionExecutor executor,
    string functionName,
    string promptName,
    string? promptTitle,
    string? promptDescription,
    IReadOnlyList<PromptArgument>? arguments,
    IList<Icon>? icons,
    IReadOnlyDictionary<string, object?> metadata) : IListener, IMcpPrompt
{
    public ITriggeredFunctionExecutor Executor { get; } = executor;

    public string FunctionName { get; } = functionName;

    public string Name { get; } = promptName;

    public string? Title { get; } = promptTitle;

    public string? Description { get; } = promptDescription;

    public IReadOnlyList<PromptArgument>? Arguments { get; } = arguments;

    public IList<Icon>? Icons { get; } = icons;

    public IReadOnlyDictionary<string, object?> Metadata { get; } = metadata;

    public void Dispose() { }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Cancel() { }

    public async Task<GetPromptResult> GetAsync(RequestContext<GetPromptRequestParams> getPromptRequest, CancellationToken cancellationToken)
    {
        var execution = new GetPromptExecutionContext(getPromptRequest);

        var input = new TriggeredFunctionData
        {
            TriggerValue = execution
        };

        var result = await Executor.TryExecuteAsync(input, cancellationToken);

        if (!result.Succeeded)
        {
            throw result.Exception;
        }

        var promptResult = await execution.ResultTask;
        return promptResult;
    }
}
