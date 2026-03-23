// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Resolves tool properties from explicitly configured options
/// set via <c>WithProperty(...)</c>.
/// </summary>
internal class ConfiguredToolPropertiesResolver(IOptionsMonitor<ToolOptions> toolOptionsMonitor) : IToolPropertiesResolver
{
    public bool TryResolve(string toolName, IFunctionMetadata functionMetadata, out List<ToolProperty>? toolProperties)
    {
        toolProperties = null;

        var toolOptions = toolOptionsMonitor.Get(toolName);

        if (toolOptions.Properties.Count == 0)
        {
            return false;
        }

        toolProperties = toolOptions.Properties;
        return true;
    }
}
