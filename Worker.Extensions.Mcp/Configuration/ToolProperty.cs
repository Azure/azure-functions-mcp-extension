using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public class ToolProperty(string name, string type, string? description)
{
    [JsonPropertyName("propertyName")]
    public string Name { get; set; } = name;

    [JsonPropertyName("propertyType")]
    public string Type { get; set; } = type;

    [JsonPropertyName("description")]
    public string? Description { get; set; } = description;
}