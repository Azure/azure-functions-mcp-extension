using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(Startup))]

namespace Microsoft.Azure.Functions.Extensions.Mcp
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddSingleton<IFunctionProvider, McpFunctionProvider>();
            builder.Services.AddSingleton<IMcpRequestHandler, DefaultMcpRequestHandler>();

            builder.AddExtension<McpExtensionConfigProvider>();
        }
    }
}