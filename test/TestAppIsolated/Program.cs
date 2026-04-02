using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Configure OpenTelemetry tracing with the MCP SDK ActivitySource.
// "Experimental.ModelContextProtocol" is the ActivitySource published by the MCP SDK (>=1.2.0).
// Requires host.json "telemetryMode": "OpenTelemetry" and APPLICATIONINSIGHTS_CONNECTION_STRING env var.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("Experimental.ModelContextProtocol");

        var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(connectionString))
        {
            tracing.AddAzureMonitorTraceExporter(o => o.ConnectionString = connectionString);
        }
    });

builder.ConfigureMcpResource("file://logo.png")
        .WithMetadata("ui", new { prefersBorder = true });

builder.ConfigureMcpTool("RenderImage")
        .WithMetadata("imageVersion", "1.0")
        .WithMetadata("source", "google");

builder.Build().Run();
