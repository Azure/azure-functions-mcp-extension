// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp
{
    public record SessionContext (string ClientId, string InstanceId)
    {
    }
}
