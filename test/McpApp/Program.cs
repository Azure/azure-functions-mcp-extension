using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.ConfigureMcpTool("GreetingTool")
    .WithProperty("name", McpToolPropertyType.String, "user's name")
    .AsMcpApp("assets/greeting.html");

builder.Build().Run();
