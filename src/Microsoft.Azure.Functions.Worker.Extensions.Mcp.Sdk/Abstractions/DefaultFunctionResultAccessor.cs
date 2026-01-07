// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk;

/// <summary>
/// Default implementation of IFunctionResultAccessor that uses the extension methods.
/// </summary>
internal sealed class DefaultFunctionResultAccessor : IFunctionResultAccessor
{
    public object? GetResult(FunctionContext context)
    {
        return context.GetInvocationResult()?.Value;
    }

    public void SetResult(FunctionContext context, object? value)
    {
        var invocationResult = context.GetInvocationResult();
        invocationResult?.Value = value;
    }
}
