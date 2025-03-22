using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;


namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

[ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
public sealed class McpToolTriggerAttribute(string name, string? description = null) : TriggerBindingAttribute
{

    /// <summary>
    /// Gets or sets the name of the MCP tool.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets or sets the description of the MCP tool.
    /// </summary>
    public string? Description { get; set; } = description;
}


