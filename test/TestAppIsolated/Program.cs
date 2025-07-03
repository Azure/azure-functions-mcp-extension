using System.Diagnostics;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

#if DEBUG
    Console.WriteLine($"Azure Functions .NET Worker (PID: {Environment.ProcessId}) initialized in debug mode.");
#endif

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
