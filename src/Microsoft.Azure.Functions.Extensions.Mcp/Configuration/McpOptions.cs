namespace Microsoft.Azure.Functions.Extensions.Mcp.Configuration;

/// <summary>
/// Options for configuring the Model Context Protocol (MCP) extension in Azure Functions.
/// </summary>
public sealed class McpOptions
{
    /// <summary>
    /// Gets or sets the name of the server that is returned to the client in the initialization response.
    /// </summary>
    public string ServerName { get; set; } = "Azure Functions MCP server";

    /// <summary>
    /// Gets or sets the server version that is returned to the client in the initialization response.
    /// </summary>
    public string ServerVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the instructions returned to the client in the initialization response.
    /// </summary>
    public string? Instructions { get; set; }
}