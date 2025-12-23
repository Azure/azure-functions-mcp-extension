// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal static class McpToolExtensions
{
    internal static JsonElement GetPropertiesInputSchema(this IMcpTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        return tool.ToolInputSchema.GetSchemaElement();
    }
}
