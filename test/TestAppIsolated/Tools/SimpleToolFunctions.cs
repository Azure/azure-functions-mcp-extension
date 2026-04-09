// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated.Tools;

/// <summary>
/// Simple tool functions that test basic trigger binding, single parameters,
/// default values, and void return types.
/// </summary>
public class SimpleToolFunctions(ILogger<SimpleToolFunctions> logger)
{
    /// <summary>
    /// Echoes a single string argument back. Tests basic single-parameter tool binding.
    /// </summary>
    [Function(nameof(EchoTool))]
    public string EchoTool(
        [McpToolTrigger(nameof(EchoTool), "Echoes a single argument back to the caller.")] ToolInvocationContext context,
        [McpToolProperty("message", "The message to echo.")] string message)
    {
        logger.LogInformation("EchoTool invoked with message: {Message}", message);
        return message ?? "(null)";
    }

    /// <summary>
    /// Echoes a single string argument with a default value. Tests optional/default parameter binding.
    /// </summary>
    [Function(nameof(EchoWithDefault))]
    public string EchoWithDefault(
        [McpToolTrigger(nameof(EchoWithDefault), "Echoes an argument, using a default if not provided.")] ToolInvocationContext context,
        [McpToolProperty("message", "The message to echo.")] string message = "(default-message)")
    {
        logger.LogInformation("EchoWithDefault invoked with message: {Message}", message);
        return message;
    }

    /// <summary>
    /// A tool that performs a side effect and returns void. Tests void return type handling.
    /// </summary>
    [Function(nameof(VoidTool))]
    public void VoidTool(
        [McpToolTrigger(nameof(VoidTool), "Performs a side effect with no return value.")] ToolInvocationContext context,
        [McpToolProperty("input", "Input to log.")] string input)
    {
        logger.LogInformation("VoidTool invoked with input: {Input}", input);
    }
}
