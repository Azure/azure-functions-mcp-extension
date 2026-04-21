// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated.Tools;

/// <summary>
/// Tools whose properties are defined entirely via the fluent builder API in Program.cs,
/// not via [McpToolProperty] attributes on parameters.
/// </summary>
public class FluentToolFunctions(ILogger<FluentToolFunctions> logger)
{
    /// <summary>
    /// A tool whose output schema is defined via ConfigureMcpTool().WithOutputSchema() in Program.cs.
    /// Tests the fluent output schema API.
    /// </summary>
    [Function(nameof(FluentOutputSchemaTool))]
    public string FluentOutputSchemaTool(
        [McpToolTrigger(nameof(FluentOutputSchemaTool), "A tool with an output schema defined via the fluent builder API.")] ToolInvocationContext context,
        [McpToolProperty("query", "The search query.")] string query)
    {
        logger.LogInformation("FluentOutputSchemaTool invoked with query: {Query}", query);
        return $"{{\"results\": [\"result1\", \"result2\"], \"query\": \"{query}\"}}";
    }

    /// <summary>
    /// A tool whose input properties are defined via ConfigureMcpTool().WithProperty() in Program.cs.
    /// Tests the fluent property definition API without attribute-based parameters.
    /// </summary>
    [Function(nameof(FluentDefinedTool))]
    public string FluentDefinedTool(
        [McpToolTrigger(nameof(FluentDefinedTool), "A tool with properties defined via the fluent builder API.")] ToolInvocationContext context)
    {
        logger.LogInformation("FluentDefinedTool invoked");

        var arguments = context.Arguments;
        var city = arguments?.TryGetValue("city", out var cityVal) == true ? cityVal?.ToString() : "(unknown)";
        var zipCode = arguments?.TryGetValue("zipCode", out var zipVal) == true ? zipVal?.ToString() : "(unknown)";
        return $"City: {city} | ZipCode: {zipCode}";
    }
}
