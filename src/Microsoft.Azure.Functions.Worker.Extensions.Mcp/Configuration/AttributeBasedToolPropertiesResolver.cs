// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Resolves tool properties by reflecting over the function's method signature
/// and extracting properties from attributes.
/// </summary>
internal class AttributeBasedToolPropertiesResolver : IToolPropertiesResolver
{
    public bool TryResolve(string toolName, IFunctionMetadata functionMetadata, out List<ToolProperty>? toolProperties)
    {
        return ToolPropertyParser.TryGetPropertiesFromAttributes(functionMetadata, out toolProperties);
    }
}
