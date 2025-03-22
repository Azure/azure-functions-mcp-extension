using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.Functions.Extensions.Mcp
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public sealed class McpToolPropertyAttribute(string name, string propertyType, string description) : Attribute, IMcpToolProperty
    {
        public string Name { get; set; } = name;

        public string PropertyType { get; set; } = propertyType;

        public string? Description { get; set; } = description;
    }
}
