using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

// Example: WithInputSchema<T>() — generate schema from a CLR type
builder.ConfigureMcpTool("savesnippet")
    .WithInputSchema<SaveSnippetInput>();

// Example: WithInputSchema(string) — pass an explicit JSON schema string
builder.ConfigureMcpTool("searchsnippets")
    .WithInputSchema("""
    {
        "type": "object",
        "properties": {
            "Pattern": { "type": "string", "description": "Pattern to search for" },
            "CaseSensitive": { "type": "boolean", "description": "Whether search is case sensitive" }
        },
        "required": ["Pattern"]
    }
    """);

// Example: WithInputSchema(JsonNode) — pass a pre-built JsonNode
var schemaNode = JsonNode.Parse("""
{
    "type": "object",
    "properties": {
        "data": { "type": "string", "description": "Base64-encoded image data" },
        "mimeType": { "type": "string", "description": "Mime type" }
    },
    "required": ["data"]
}
""");

builder.ConfigureMcpTool("RenderImage")
    .WithInputSchema(schemaNode!);

// Example: WithProperty() — define properties individually
builder.ConfigureMcpTool("GetFunctionsLogo")
    .WithProperty("format", McpToolPropertyType.String, "The desired image format", required: false);

builder.Build().Run();

// Model used with WithInputSchema<T>()
public class SaveSnippetInput
{
    public required string Name { get; set; }
    public string? Content { get; set; }
}
