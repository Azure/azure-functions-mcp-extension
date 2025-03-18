using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;


namespace Microsoft.Azure.Functions.Worker
{
    [ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
    public sealed class McpToolTriggerAttribute : TriggerBindingAttribute
    {

        /// <summary>
        /// Gets or sets the name of the MCP tool.
        /// </summary>
        public bool Name { get; set; }

    }
}