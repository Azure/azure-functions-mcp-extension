using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Model;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using System.Text.Json;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

[Extension("Mcp")]
internal sealed class McpExtensionConfigProvider(IToolRegistry toolRegistry) : IExtensionConfigProvider
{
    public void Initialize(ExtensionConfigContext context)
    {
        context.AddBindingRule<McpToolTriggerAttribute>()
            .BindToTrigger(new McpTriggerBindingProvider(toolRegistry));

        context.AddBindingRule<McpToolPropertyAttribute>()
            .AddConverter<ToolInvocationContext, string>(InvocationContextToString)
            .AddConverter<CallToolExecutionContext, string>(CallToolExecutionContextToString)
            .Bind(new McpToolPropertyBindingProvider());

    }

    private static string CallToolExecutionContextToString(CallToolExecutionContext executionContext)
        => InvocationContextToString(executionContext.Request);

    private static string InvocationContextToString(ToolInvocationContext context)
        => JsonSerializer.Serialize(context, McpJsonSerializerOptions.DefaultOptions);
}