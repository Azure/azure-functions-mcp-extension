using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Model;

/// <summary>
/// Represents the roots capability configuration.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/2024-11-05/schema.json">See the schema for details</see>
/// </summary>
public record RootsCapability
{
    /// <summary>
    /// Whether the client supports notifications for changes to the roots list.
    /// </summary>
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; init; }

    /// <summary>Gets or sets the handler for sampling requests.</summary>
    [JsonIgnore]
    public Func<ListRootsRequestParams?, CancellationToken, Task<ListRootsResult>>? RootsHandler { get; init; }
}