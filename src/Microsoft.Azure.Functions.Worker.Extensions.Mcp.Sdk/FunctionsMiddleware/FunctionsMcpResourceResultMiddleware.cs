// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk;
using Microsoft.Azure.Functions.Worker.Middleware;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal sealed class FunctionsMcpResourceResultMiddleware : IFunctionsWorkerMiddleware
{
    private const string ResourceInvocationContextKey = "ResourceInvocationContext";
    private readonly IFunctionResultAccessor _resultAccessor;

    public FunctionsMcpResourceResultMiddleware(IFunctionResultAccessor? resultAccessor = null)
    {
        _resultAccessor = resultAccessor ?? new DefaultFunctionResultAccessor();
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);

        await next(context);

        if (!IsMcpResourceInvocation(context))
        {
            return;
        }

        var functionResult = _resultAccessor.GetResult(context);
        if (functionResult is null || HasOutputBindings(context))
        {
            return;
        }

        if (functionResult is ResourceContents)
        {
            throw new InvalidOperationException("Direct returns of TextResourceContents or BlobResourceContents are not supported for MCP resources. Return string, byte[], or FileResourceContents instead.");
        }

        if (functionResult is not FileResourceContents)
        {
            return;
        }

        var content = JsonSerializer.Serialize(functionResult, McpJsonUtilities.DefaultOptions);
        var envelope = new ResourceResultEnvelope
        {
            Content = content
        };

        _resultAccessor.SetResult(context, JsonSerializer.Serialize(envelope));
    }

    private static bool IsMcpResourceInvocation(FunctionContext context)
    {
        return context.Items.ContainsKey(ResourceInvocationContextKey);
    }

    private static bool HasOutputBindings(FunctionContext context)
    {
        return context.FunctionDefinition.OutputBindings.Any();
    }

    private sealed class ResourceResultEnvelope
    {
        public required string Content { get; init; }
    }
}