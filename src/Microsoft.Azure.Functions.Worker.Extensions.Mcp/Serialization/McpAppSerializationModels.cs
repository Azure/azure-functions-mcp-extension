// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Serialization model for the MCP App resource result returned by the middleware.
/// Matches the shape expected by the host's ResourceReturnValueBinder.
/// </summary>
internal sealed class McpAppResourceResult
{
    [JsonPropertyName("content")]
    public required string Content { get; set; }
}

/// <summary>
/// Serialization model for resource contents with <c>_meta</c> metadata,
/// matching the MCP protocol's <c>resources/read</c> response shape.
/// </summary>
internal sealed class McpAppResourceContent
{
    [JsonPropertyName("uri")]
    public required string Uri { get; set; }

    [JsonPropertyName("mimeType")]
    public required string MimeType { get; set; }

    [JsonPropertyName("text")]
    public required string Text { get; set; }

    [JsonPropertyName("_meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpAppResourceMeta? Meta { get; set; }
}

/// <summary>
/// The <c>_meta</c> envelope containing the <c>ui</c> key.
/// </summary>
internal sealed class McpAppResourceMeta
{
    [JsonPropertyName("ui")]
    public McpAppUiMeta? Ui { get; set; }
}

/// <summary>
/// The <c>_meta.ui</c> object for a resource response, per the MCP Apps spec (SEP-1865).
/// Contains CSP, permissions, border preference, and domain.
/// </summary>
internal sealed class McpAppUiMeta
{
    [JsonPropertyName("csp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpAppCsp? Csp { get; set; }

    [JsonPropertyName("permissions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpAppPermissionsMap? Permissions { get; set; }

    [JsonPropertyName("prefersBorder")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? PrefersBorder { get; set; }

    [JsonPropertyName("domain")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Domain { get; set; }
}

/// <summary>
/// CSP configuration matching the MCP Apps spec field names.
/// </summary>
internal sealed class McpAppCsp
{
    [JsonPropertyName("connectDomains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ConnectDomains { get; set; }

    [JsonPropertyName("resourceDomains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ResourceDomains { get; set; }

    [JsonPropertyName("frameDomains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? FrameDomains { get; set; }

    [JsonPropertyName("baseUriDomains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? BaseUriDomains { get; set; }
}

/// <summary>
/// Permissions map using empty objects as values per the MCP Apps spec.
/// </summary>
internal sealed class McpAppPermissionsMap
{
    [JsonPropertyName("clipboardRead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EmptyObject? ClipboardRead { get; set; }

    [JsonPropertyName("clipboardWrite")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EmptyObject? ClipboardWrite { get; set; }
}

/// <summary>
/// Represents an empty JSON object <c>{}</c> used as a permission value.
/// </summary>
internal sealed class EmptyObject { }
