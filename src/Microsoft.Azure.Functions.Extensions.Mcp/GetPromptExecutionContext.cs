// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class GetPromptExecutionContext(RequestContext<GetPromptRequestParams> requestContext)
{
    private readonly TaskCompletionSource<GetPromptResult> _taskCompletionSource = new();

    public GetPromptRequestParams Request => requestContext.Params!;

    public RequestContext<GetPromptRequestParams> RequestContext => requestContext;

    public Task<GetPromptResult> ResultTask => _taskCompletionSource.Task;

    public void SetResult(GetPromptResult result)
    {
        _taskCompletionSource.SetResult(result);
    }
}
