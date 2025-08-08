// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface ISseRequestHandler
{
    Task HandleRequest(HttpContext context);

    bool IsLegacySseRequest(HttpContext context);
}
