// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;

public interface IMcpRequestHandler
{
    Task HandleRequest(HttpContext context);
}
