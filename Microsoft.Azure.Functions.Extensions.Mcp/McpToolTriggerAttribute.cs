using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

[AttributeUsage(AttributeTargets.Parameter)]
#pragma warning disable CS0618 // Type or member is obsolete
[Binding(TriggerHandlesReturnValue = true)]
#pragma warning restore CS0618 // Type or member is obsolete
public sealed class McpToolTriggerAttribute(string name, string? description) : Attribute
{
    public string Name { get; } = name;

    public string? Description { get; set; } = description;
}