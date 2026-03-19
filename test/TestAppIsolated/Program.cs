using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

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

builder.ConfigureMcpResource("file://logo.png")
        .WithMetadata("ui", new { prefersBorder = true });

builder.ConfigureMcpTool("RenderImage")
        .WithMetadata("imageVersion", "1.0")
        .WithMetadata("source", "google");

builder.Build().Run();
