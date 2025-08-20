// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Represents the context of an MCP session
/// </summary>
internal sealed record SessionContext(string ClientId, string InstanceId);
