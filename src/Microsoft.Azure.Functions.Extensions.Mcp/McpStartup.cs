// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

[assembly: WebJobsStartup(typeof(McpStartup))]

namespace Microsoft.Azure.Functions.Extensions.Mcp
{
    public class McpStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddMcp();
        }
    }
}
