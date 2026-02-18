using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.ConfigureMcpResource("file://logo.png")
        .WithMeta("ui", new { prefersBorder = true });

builder.ConfigureMcpTool("RenderImage")
        .WithMeta("imageVersion", "1.0")
        .WithMeta("source", "google");

builder.Build().Run();
