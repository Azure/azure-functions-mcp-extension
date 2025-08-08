// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class StreamableHttpRequestHandler : IStreamableHttpRequestHandler
{
    private static readonly JsonTypeInfo<JsonRpcError> ErrorTypeInfo = (JsonTypeInfo<JsonRpcError>)McpJsonUtilities.DefaultOptions.GetTypeInfo(typeof(JsonRpcError));

    public Task HandleRequest(HttpContext context)
    {
        throw new NotImplementedException();
    }
}
