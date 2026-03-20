// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Validation;
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
                                      ToolInputSchema requestHandler,
                                      IReadOnlyDictionary<string, object?> metadata,
                                      JsonElement? outputSchema = null) : IListener, IMcpTool
{
    public ITriggeredFunctionExecutor Executor { get; } = executor;

    public string FunctionName { get; } = functionName;

    public string Name { get; } = toolName;

    public string? Description { get; set; } = toolDescription;

    public IReadOnlyDictionary<string, object?> Metadata { get; } = metadata;

    public ToolInputSchema InputSchema { get; } = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));

    public JsonElement? OutputSchema { get; } = outputSchema;

    public void Dispose()
    {
        // Dispose the validator if it implements IDisposable (e.g., JsonSchemaToolInputSchema)
        if (InputSchema is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Cancel() { }

    public async Task<CallToolResult> RunAsync(RequestContext<CallToolRequestParams> callToolRequest, CancellationToken cancellationToken)
    {
        // Validate required properties are present in the incoming request.
        InputSchema.Validate(callToolRequest.Params);

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
            // Validate structured content against the output schema, if one was declared.
            if (OutputSchema is JsonElement schema)
            {
                if (callToolResult.StructuredContent is not System.Text.Json.Nodes.JsonObject structuredContent)
                {
                    throw new McpProtocolException(
                        "Output schema is declared but the tool result does not contain structured content. " +
                        "When an output schema is provided, the tool must return structured content conforming to the schema.",
                        McpErrorCode.InvalidParams);
                }

                var validationResult = JsonSchemaValidator.Validate(schema, structuredContent);
                if (!validationResult.IsValid)
                {
                    throw new McpProtocolException(
                        $"Structured content does not conform to the output schema. {validationResult.ErrorMessage}",
                        McpErrorCode.InvalidParams);
                }
            }

            return callToolResult;
        }

        // We did not receive a CallToolResult from the function execution,
        // return an empty result.
        return new CallToolResult { Content = [] };
    }
}
