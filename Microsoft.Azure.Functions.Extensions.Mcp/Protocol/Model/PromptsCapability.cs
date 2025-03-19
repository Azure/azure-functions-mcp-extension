using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Model;

/// <summary>
/// Represents the prompts capability configuration.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/2024-11-05/schema.json">See the schema for details</see>
/// </summary>
public record PromptsCapability
{
    /// <summary>
    /// Whether this server supports notifications for changes to the prompt list.
    /// </summary>
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; init; }
}