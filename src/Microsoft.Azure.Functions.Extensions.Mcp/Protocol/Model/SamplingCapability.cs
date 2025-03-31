using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Model;

/// <summary>
/// Represents the sampling capability configuration.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/2024-11-05/schema.json">See the schema for details</see>
/// </summary>
public record SamplingCapability
{
    // Currently empty in the spec, but may be extended in the future

    /// <summary>Gets or sets the handler for sampling requests.</summary>
    [JsonIgnore]
    public Func<CreateMessageRequestParams?, CancellationToken, Task<CreateMessageResult>>? SamplingHandler { get; init; }
}