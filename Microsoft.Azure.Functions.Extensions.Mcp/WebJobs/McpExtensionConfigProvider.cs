using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace Microsoft.Azure.Functions.Extensions.Mcp.WebJobs;

[Extension("Mcp")]
internal class McpExtensionConfigProvider(IToolRegistry toolRegistry) : IExtensionConfigProvider
{
    public void Initialize(ExtensionConfigContext context)
    {
        context.AddBindingRule<McpToolTriggerAttribute>()
            .BindToTrigger(new McpTriggerBindingProvider(toolRegistry));
    }
}