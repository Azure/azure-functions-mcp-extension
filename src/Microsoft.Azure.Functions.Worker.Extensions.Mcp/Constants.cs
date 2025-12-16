// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal static class Constants
{
    public const string ToolInvocationContextKey = "ToolInvocationContext";
    public const string ResourceInvocationContextKey = "ResourceInvocationContext";

    public const string McpToolTriggerBindingType = "mcpToolTrigger";
    public const string McpResourceTriggerBindingType = "mcpResourceTrigger";

    public const string McpToolPropertyBindingType = "mcpToolProperty";

    public const string McpToolPropertyType = "propertyType";

    public const string McpToolPropertyName = "propertyName";
}
