// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ChatGptSampleApp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Register in-memory workout repository (no Azurite needed)
        services.AddSingleton<IWorkoutRepository, InMemoryWorkoutRepository>();
    })
    .Build();

host.Run();
