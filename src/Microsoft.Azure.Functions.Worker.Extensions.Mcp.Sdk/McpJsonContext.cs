// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(McpToolResult))]
[JsonSerializable(typeof(McpPromptResult))]
internal sealed partial class McpJsonContext : JsonSerializerContext
{
}
