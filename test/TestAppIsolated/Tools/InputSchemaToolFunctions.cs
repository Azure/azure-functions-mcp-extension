// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated.Tools;

/// <summary>
/// Tools whose input schema is defined via <c>WithInputSchema</c> in Program.cs.
/// </summary>
public class InputSchemaToolFunctions(ILogger<InputSchemaToolFunctions> logger)
{
    /// <summary>
    /// A tool with an explicit input schema set via <c>ConfigureMcpTool().WithInputSchema()</c>.
    /// The worker emits the schema and signals the host to use it directly.
    /// </summary>
    [Function(nameof(InputSchemaTool))]
    public string InputSchemaTool(
        [McpToolTrigger(nameof(InputSchemaTool), "A tool with an explicit input schema.")] ToolInvocationContext context)
    {
        logger.LogInformation("InputSchemaTool invoked");

        var arguments = context.Arguments;
        var location = arguments?.TryGetValue("location", out var locVal) == true ? locVal?.ToString() : "(unknown)";
        var units = arguments?.TryGetValue("units", out var unitsVal) == true ? unitsVal?.ToString() : "celsius";
        return $"Weather for {location} in {units}";
    }
}
