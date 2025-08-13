// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IStreamableHttpRequestHandler
{
    Task HandleRequestAsync(HttpContext context);
}
