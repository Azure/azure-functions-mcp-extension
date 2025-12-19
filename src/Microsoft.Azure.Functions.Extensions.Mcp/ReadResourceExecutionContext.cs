// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class ReadResourceExecutionContext(RequestContext<ReadResourceRequestParams> requestContext)
{
    private readonly TaskCompletionSource<ReadResourceResult> _taskCompletionSource = new();

    public ReadResourceRequestParams Request => RequestContext.Params!;

    public RequestContext<ReadResourceRequestParams> RequestContext => requestContext;

    public Task<ReadResourceResult> ResultTask => _taskCompletionSource.Task;

    public void SetResult(ReadResourceResult result)
    {
        _taskCompletionSource.SetResult(result);
    }
}