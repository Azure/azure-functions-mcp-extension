// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol.Types;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal class CallToolExecutionContext(CallToolRequestParams request)
{
    private readonly TaskCompletionSource<object?> _taskCompletionSource = new();

    public CallToolRequestParams Request { get; init; } = request;

    public Task<object?> ResultTask => _taskCompletionSource.Task;

    public void SetResult(object result)
    {
        _taskCompletionSource.TrySetResult(result);
    }
}
