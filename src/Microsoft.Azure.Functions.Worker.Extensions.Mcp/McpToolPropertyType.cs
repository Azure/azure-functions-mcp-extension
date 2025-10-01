// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;

internal sealed record McpToolPropertyType(string TypeName, bool IsArray = false)
{
    private static McpToolPropertyType? _string;
    private static McpToolPropertyType? _number;
    private static McpToolPropertyType? _integer;
    private static McpToolPropertyType? _boolean;
    private static McpToolPropertyType? _object;

    public static McpToolPropertyType String => _string ??= new("string");

    public static McpToolPropertyType Number => _number ??= new("number");

    public static McpToolPropertyType Integer => _integer ??= new("integer");

    public static McpToolPropertyType Boolean => _boolean ??= new("boolean");

    public static McpToolPropertyType Object => _object ??= new("object");

    public McpToolPropertyType AsArray() => new(TypeName, true);
}
