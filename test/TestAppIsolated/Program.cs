using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.ConfigureMcpResource("file://logo.png")
        .WithMetadata("ui", new { prefersBorder = true });

builder.ConfigureMcpTool("RenderImage")
        .WithMetadata("imageVersion", "1.0")
        .WithMetadata("source", "google");

builder.Build().Run();
