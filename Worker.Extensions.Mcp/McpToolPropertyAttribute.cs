using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

[ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
public sealed class McpToolPropertyAttribute(string name, string propertyType, string description) : InputBindingAttribute
{
    public string Name { get; set; } = name;

    public string PropertyType { get; set; } = propertyType;

    public string? Description { get; set; } = description;
}