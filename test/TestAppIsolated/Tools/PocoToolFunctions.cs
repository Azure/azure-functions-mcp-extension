// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated.Tools;

/// <summary>
/// Tools that test POCO trigger binding (complex objects as trigger input)
/// and the [McpContent] attribute for structured content output.
/// </summary>
public class PocoToolFunctions(ILogger<PocoToolFunctions> logger)
{
    /// <summary>
    /// Tests POCO as the trigger parameter (not ToolInvocationContext).
    /// The PersonRequest object IS the trigger — the runtime deserializes tool arguments into it.
    /// Example: { "name": "Alice", "age": 30, "isPremium": true }
    /// </summary>
    [Function(nameof(PocoInputTool))]
    public string PocoInputTool(
        [McpToolTrigger(nameof(PocoInputTool), "Accepts a POCO object as input and returns a greeting.")] PersonRequest request,
        ToolInvocationContext context)
    {
        logger.LogInformation("PocoInputTool invoked for {Name}", request.Name);
        return $"Hello, {request.Name}! You are {request.Age} years old. Premium: {request.IsPremium}";
    }

    /// <summary>
    /// Tests [McpContent]-decorated POCO as the return type.
    /// When returned, the runtime serializes it as both text content (backwards compat)
    /// and structured content (for clients that support it).
    /// Example: { "city": "Seattle" }
    /// </summary>
    [Function(nameof(PocoOutputTool))]
    public WeatherResult PocoOutputTool(
        [McpToolTrigger(nameof(PocoOutputTool), "Returns a weather result as structured content.")] ToolInvocationContext context,
        [McpToolProperty("city", "The city to get weather for.", true)] string city)
    {
        logger.LogInformation("PocoOutputTool invoked for {City}", city);
        return new WeatherResult
        {
            City = city,
            Temperature = 72,
            Condition = "Sunny"
        };
    }

    public class PersonRequest
    {
        [Description("The person's name")]
        public required string Name { get; set; }

        [Description("The person's age")]
        public int Age { get; set; }

        [Description("Whether the person has premium access")]
        public bool IsPremium { get; set; }
    }

    /// <summary>
    /// Decorated with [McpContent] to auto-generate text + structured content on return.
    /// </summary>
    [McpContent]
    public class WeatherResult
    {
        [Description("The city name")]
        public required string City { get; set; }

        [Description("Temperature in Fahrenheit")]
        public int Temperature { get; set; }

        [Description("Weather condition")]
        public string? Condition { get; set; }
    }
}
