using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(McpStartup))]

namespace Microsoft.Azure.Functions.Extensions.Mcp
{
    public class McpStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddSingleton<IFunctionProvider, McpFunctionProvider>();

            builder.AddExtension<McpExtensionConfigProvider>();
        }
    }
}