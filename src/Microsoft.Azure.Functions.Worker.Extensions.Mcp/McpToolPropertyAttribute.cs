using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

[ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
public sealed class McpToolPropertyAttribute(string propertyName, string propertyType, string description) : InputBindingAttribute
{
    public McpToolPropertyAttribute()
    : this(string.Empty, string.Empty, string.Empty)
    {
    }

    public string PropertyName { get; set; } = propertyName;

    public string PropertyType { get; set; } = propertyType;

    public string? Description { get; set; } = description;
}