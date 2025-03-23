using System.Text.Json.Serialization;


namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// The context for the tool call.
/// </summary>
public class ToolInvocationContext
{
    /// <summary>
    /// Tool name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Optional arguments to pass to the tool.
    /// </summary>
    [JsonPropertyName("arguments")]
    public Dictionary<string, object>? Arguments { get; init; }
}