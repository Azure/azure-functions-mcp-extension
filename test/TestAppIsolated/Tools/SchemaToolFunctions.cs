// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TestAppIsolated.Tools;

/// <summary>
/// Tools with explicit input and output schemas configured via fluent builder in Program.cs.
/// </summary>
public class SchemaToolFunctions(ILogger<SchemaToolFunctions> logger)
{
    /// <summary>
    /// A tool with explicit input and output schemas set via fluent builder in Program.cs.
    /// Tests schema configuration and emission.
    /// </summary>
    [Function(nameof(SchemaTool))]
    public string SchemaTool(
        [McpToolTrigger(nameof(SchemaTool), "A tool with explicit input and output schemas.")] ToolInvocationContext context)
    {
        logger.LogInformation("SchemaTool invoked");

        var arguments = context.Arguments;
        var location = arguments?.TryGetValue("location", out var locVal) == true ? locVal?.ToString() : "(unknown)";
        var units = arguments?.TryGetValue("units", out var unitsVal) == true ? unitsVal?.ToString() : "celsius";

        return JsonSerializer.Serialize(new
        {
            location,
            units,
            forecast = $"Weather for {location} in {units}"
        });
    }
}
