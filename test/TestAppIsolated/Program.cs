using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.ConfigureMcpTool("MyTestFunction")
    .WithProperty("userId", McpToolPropertyType.String, "User ID", required: true)
    .WithMetadata("openai/outputTemplate", "ui://widget/welcome.html")
    .WithMetadata("openai/widgetCSP", new JsonObject
    {
        ["connect_domains"] = new JsonArray(),
        ["resource_domains"] = new JsonArray()
    });

builder.Build().Run();
