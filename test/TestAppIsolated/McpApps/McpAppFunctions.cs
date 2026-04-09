// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated.McpApps;

/// <summary>
/// MCP App tool functions that test the AsMcpApp() fluent builder API
/// with varying levels of configuration.
/// </summary>
public class McpAppFunctions(ILogger<McpAppFunctions> logger)
{
    /// <summary>
    /// A fully configured MCP App with view, title, permissions, and CSP.
    /// Tests the full AsMcpApp() builder chain.
    /// Configured in Program.cs via ConfigureMcpTool("HelloApp").AsMcpApp(...).
    /// </summary>
    [Function(nameof(HelloApp))]
    public string HelloApp(
        [McpToolTrigger(nameof(HelloApp), "A fully configured MCP App that says hello.")] ToolInvocationContext context)
    {
        logger.LogInformation("HelloApp MCP App invoked");
        return "Hello from the HelloApp MCP App";
    }

    /// <summary>
    /// A minimal MCP App with only a view (no permissions, CSP, or visibility).
    /// Tests the bare-minimum AsMcpApp() configuration.
    /// Configured in Program.cs via ConfigureMcpTool("MinimalApp").AsMcpApp(...).
    /// </summary>
    [Function(nameof(MinimalApp))]
    public string MinimalApp(
        [McpToolTrigger(nameof(MinimalApp), "A minimal MCP App with view only.")] ToolInvocationContext context)
    {
        logger.LogInformation("MinimalApp MCP App invoked");
        return "Hello from the MinimalApp MCP App";
    }

    /// <summary>
    /// An MCP App with visibility configuration.
    /// Tests WithVisibility() on the AsMcpApp() builder.
    /// Configured in Program.cs via ConfigureMcpTool("VisibilityApp").AsMcpApp(...).
    /// </summary>
    [Function(nameof(VisibilityApp))]
    public string VisibilityApp(
        [McpToolTrigger(nameof(VisibilityApp), "An MCP App with visibility configuration.")] ToolInvocationContext context)
    {
        logger.LogInformation("VisibilityApp MCP App invoked");
        return "Hello from the VisibilityApp MCP App";
    }
}
