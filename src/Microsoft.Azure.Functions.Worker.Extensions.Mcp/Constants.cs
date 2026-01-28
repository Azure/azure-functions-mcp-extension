// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal static class Constants
{
    // Tool constants
    public const string ToolInvocationContextKey = "ToolInvocationContext";
    public const string McpToolTriggerBindingType = "mcpToolTrigger";
    public const string McpToolPropertyBindingType = "mcpToolProperty";
    public const string McpToolPropertyType = "propertyType";
    public const string McpToolPropertyName = "propertyName";

    // Resource constants
    public const string ResourceInvocationContextKey = "ResourceInvocationContext";
    public const string McpResourceTriggerBindingType = "mcpResourceTrigger";
}
