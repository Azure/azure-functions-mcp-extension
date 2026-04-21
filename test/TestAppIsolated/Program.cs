// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// ── MCP Apps ────────────────────────────────────────────────────────────────

// Full MCP App: view + title + permissions + CSP
builder.ConfigureMcpTool("HelloApp")
    .AsMcpApp(app => app
        .WithView("assets/hello-app.html")
        .WithTitle("Hello App")
        .WithPermissions(McpAppPermissions.ClipboardWrite | McpAppPermissions.ClipboardRead)
        .WithCsp(csp =>
        {
            csp.AllowBaseUri("https://www.microsoft.com")
            .ConnectTo("https://www.microsoft.com");
        }));

// Minimal MCP App: view only, no permissions/CSP/visibility
builder.ConfigureMcpTool("MinimalApp")
    .AsMcpApp(app => app
        .WithView("assets/minimal-app.html")
        .WithTitle("Minimal App"));

// MCP App with visibility configuration
builder.ConfigureMcpTool("VisibilityApp")
    .AsMcpApp(app => app
        .WithVisibility(McpVisibility.App)
        .WithView("assets/minimal-app.html")
        .WithTitle("Visibility App"));

// ── Tool fluent API ─────────────────────────────────────────────────────────

// Tool with metadata defined entirely via fluent builder (no [McpMetadata] attribute)
builder.ConfigureMcpTool("FluentMetadataTool")
    .WithMetadata("imageVersion", "1.0")
    .WithMetadata("source", "builder");

// Tool with properties defined entirely via fluent builder (no [McpToolProperty] attributes)
builder.ConfigureMcpTool("FluentDefinedTool")
    .WithProperty("city", McpToolPropertyType.String, "The city name.", required: true)
    .WithProperty("zipCode", McpToolPropertyType.String, "The ZIP code.", required: false);

// ── Tool with explicit input schema ─────────────────────────────────────

// Tool whose input schema is explicitly set via WithInputSchema (opt-in)
builder.ConfigureMcpTool("InputSchemaTool")
    .WithInputSchema("""
        {
            "type": "object",
            "properties": {
                "location": {
                    "type": "string",
                    "description": "The city and state, e.g. San Francisco, CA"
                },
                "units": {
                    "type": "string",
                    "description": "The unit system for temperature",
                    "enum": ["celsius", "fahrenheit"]
                }
            },
            "required": ["location"]
        }
        """);

// ── Resource fluent API ─────────────────────────────────────────────────────

// Additional metadata on an attribute-defined resource
builder.ConfigureMcpResource("file://notes.txt")
    .WithMetadata("category", "documentation")
    .WithMetadata("priority", 1);

// ── Prompt fluent API ───────────────────────────────────────────────────────

// Prompt with arguments and metadata defined via fluent builder
builder.ConfigureMcpPrompt("fluent_prompt")
    .WithArgument("query", "The search query to process", required: true)
    .WithMetadata("source", "fluent-api");

// Additional metadata on an attribute-defined prompt
builder.ConfigureMcpPrompt("metadata_prompt")
    .WithMetadata("environment", "test");

builder.Build().Run();
