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

        // Register workout repository with Azure Table Storage
        services.AddSingleton<IWorkoutRepository>(sp =>
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? "UseDevelopmentStorage=true";

            // Placeholder user ID for demo purposes
            var userId = "default-user";

            return new AzureTableWorkoutRepository(connectionString, userId);
        });
    })
    .Build();

host.Run();
