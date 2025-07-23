// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal class DefaultMcpInstanceIdProvider : IMcpInstanceIdProvider
{
    public string InstanceId { get; } = Utility.CreateId();
}
