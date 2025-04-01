using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Azure;

[assembly: WebJobsStartup(typeof(Startup))]

namespace Microsoft.Azure.Functions.Extensions.Mcp
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddMcp();
        }
    }
}