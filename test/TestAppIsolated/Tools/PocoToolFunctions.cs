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

    /// <summary>
    /// Tests [McpOutput]-decorated POCO return type via async Task&lt;T&gt;.
    /// The output schema should be auto-generated from OrderResult.
    /// Example: { "item": "Widget", "quantity": 5 }
    /// </summary>
    [Function(nameof(AsyncOutputSchemaTool))]
    public Task<OrderResult> AsyncOutputSchemaTool(
        [McpToolTrigger(nameof(AsyncOutputSchemaTool), "Returns an order result with auto-generated output schema.")] ToolInvocationContext context,
        [McpToolProperty("item", "The item to order.", true)] string item,
        [McpToolProperty("quantity", "The quantity to order.")] int quantity)
    {
        logger.LogInformation("AsyncOutputSchemaTool invoked for {Item}", item);
        return Task.FromResult(new OrderResult
        {
            Item = item,
            Quantity = quantity,
            Total = quantity * 9.99m,
            Status = "Confirmed"
        });
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
    /// Also decorated with [McpOutput] to auto-generate an output schema.
    /// </summary>
    [McpContent]
    [McpOutput]
    public class WeatherResult
    {
        [Description("The city name")]
        public required string City { get; set; }

        [Description("Temperature in Fahrenheit")]
        public int Temperature { get; set; }

        [Description("Weather condition")]
        public string? Condition { get; set; }
    }

    /// <summary>
    /// Decorated with [McpOutput] for auto-generated output schema.
    /// </summary>
    [McpOutput]
    public class OrderResult
    {
        [Description("The ordered item")]
        public required string Item { get; set; }

        [Description("The quantity ordered")]
        public int Quantity { get; set; }

        [Description("The total price")]
        public decimal Total { get; set; }

        [Description("The order status")]
        public string? Status { get; set; }
    }
}
