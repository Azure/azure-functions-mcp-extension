// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public record McpClientSessionContext(string ClientId, string InstanceId, bool IsStateless)
{
    public override string ToString() => $"ClientId: {ClientId}, InstanceId: {InstanceId}, Stateless: {IsStateless}";
}
