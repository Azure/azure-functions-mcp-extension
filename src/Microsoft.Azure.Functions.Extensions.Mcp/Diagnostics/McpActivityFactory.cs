// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

/// <summary>
/// Factory for creating Activity instances for MCP operations following OTel semantic conventions.
/// </summary>
/// <remarks>
/// This factory implements the MCP semantic conventions for distributed tracing:
/// - Extracts parent context from params._meta (W3C Trace Context)
/// - Links to the transport span 
/// - Applies semantic convention attributes to the span
/// </remarks>
internal sealed class McpActivityFactory
{
    private static readonly ActivitySource ActivitySource = new(
        SemanticConventions.ActivitySourceName,
        SemanticConventions.ActivitySourceVersion);

    private readonly ActivityHelper _activityHelper;

    public McpActivityFactory()
    {
        _activityHelper = new ActivityHelper(ActivitySource);
    }

    /// <summary>
    /// Creates an activity for a tool execution operation.
    /// </summary>
    /// <param name="toolName">The name of the tool being executed.</param>
    /// <param name="requestParams">The CallToolRequestParams (used to extract _meta context).</param>
    /// <param name="transportContext">The transport span context to link to.</param>
    /// <param name="requestContext">The request context with session, client, and protocol info.</param>
    /// <returns>The created Activity, or null if no listeners are registered.</returns>
    public Activity? CreateToolActivity(
        string toolName,
        CallToolRequestParams? requestParams,
        ActivityContext? transportContext,
        McpRequestTraceContext requestContext = default)
    {
        // Span name: "{method} {target}" per semantic conventions
        var spanName = $"{SemanticConventions.Methods.ToolsCall} {toolName}";

        // Extract parent context from _meta (null if not present)
        var parentContext = TraceContextPropagator.Extract(requestParams);

        var activity = _activityHelper.StartServerActivity(
            spanName,
            parentContext,
            transportContext,
            a => ConfigureToolActivity(a, toolName, requestContext));

        return activity;
    }

    /// <summary>
    /// Creates an activity for a resource read operation.
    /// </summary>
    /// <param name="resourceUri">The URI of the resource being read.</param>
    /// <param name="requestParams">The ReadResourceRequestParams (used to extract _meta context).</param>
    /// <param name="transportContext">The transport span context to link to.</param>
    /// <param name="requestContext">The request context with session, client, and protocol info.</param>
    /// <returns>The created Activity, or null if no listeners are registered.</returns>
    public Activity? CreateResourceActivity(
        string resourceUri,
        ReadResourceRequestParams? requestParams,
        ActivityContext? transportContext,
        McpRequestTraceContext requestContext = default)
    {
        // Span name: "{method} {target}" per semantic conventions
        // For resources, we might want to use a shortened/sanitized URI
        var spanName = $"{SemanticConventions.Methods.ResourcesRead} {SanitizeResourceUri(resourceUri)}";

        // Extract parent context from _meta (null if not present)
        var parentContext = TraceContextPropagator.Extract(requestParams);

        var activity = _activityHelper.StartServerActivity(
            spanName,
            parentContext,
            transportContext,
            a => ConfigureResourceActivity(a, resourceUri, requestContext));

        return activity;
    }

    /// <summary>
    /// Configures an activity for tool execution with semantic convention attributes.
    /// </summary>
    private static void ConfigureToolActivity(
        Activity activity,
        string toolName,
        McpRequestTraceContext context)
    {
        // Required attributes
        activity.SetTag(SemanticConventions.Mcp.MethodName, SemanticConventions.Methods.ToolsCall);
        activity.SetTag(SemanticConventions.GenAi.ToolName, toolName);

        // Recommended attributes
        activity.SetTag(SemanticConventions.GenAi.OperationName, SemanticConventions.Operations.ExecuteTool);
        activity.SetTag(SemanticConventions.JsonRpc.ProtocolVersion, SemanticConventions.JsonRpc.Version);
        activity.SetTag(SemanticConventions.Network.ProtocolName, SemanticConventions.Network.ProtocolHttp);
        activity.SetTag(SemanticConventions.Network.Transport, SemanticConventions.Network.TransportTcp);

        // Session and protocol info
        if (!string.IsNullOrEmpty(context.SessionId))
        {
            activity.SetTag(SemanticConventions.Mcp.SessionId, context.SessionId);
        }

        if (!string.IsNullOrEmpty(context.McpProtocolVersion))
        {
            activity.SetTag(SemanticConventions.Mcp.ProtocolVersion, context.McpProtocolVersion);
        }

        // Network info
        if (!string.IsNullOrEmpty(context.HttpProtocolVersion))
        {
            activity.SetTag(SemanticConventions.Network.ProtocolVersion, context.HttpProtocolVersion);
        }

        // Client info
        if (!string.IsNullOrEmpty(context.ClientAddress))
        {
            activity.SetTag(SemanticConventions.Client.Address, context.ClientAddress);
        }

        if (context.ClientPort.HasValue)
        {
            activity.SetTag(SemanticConventions.Client.Port, context.ClientPort.Value);
        }
    }

    /// <summary>
    /// Configures an activity for resource read with semantic convention attributes.
    /// </summary>
    private static void ConfigureResourceActivity(
        Activity activity,
        string resourceUri,
        McpRequestTraceContext context)
    {
        // Required attributes
        activity.SetTag(SemanticConventions.Mcp.MethodName, SemanticConventions.Methods.ResourcesRead);
        activity.SetTag(SemanticConventions.Mcp.ResourceUri, resourceUri);

        // Recommended attributes
        activity.SetTag(SemanticConventions.JsonRpc.ProtocolVersion, SemanticConventions.JsonRpc.Version);
        activity.SetTag(SemanticConventions.Network.ProtocolName, SemanticConventions.Network.ProtocolHttp);
        activity.SetTag(SemanticConventions.Network.Transport, SemanticConventions.Network.TransportTcp);

        // Session and protocol info
        if (!string.IsNullOrEmpty(context.SessionId))
        {
            activity.SetTag(SemanticConventions.Mcp.SessionId, context.SessionId);
        }

        if (!string.IsNullOrEmpty(context.McpProtocolVersion))
        {
            activity.SetTag(SemanticConventions.Mcp.ProtocolVersion, context.McpProtocolVersion);
        }

        // Network info
        if (!string.IsNullOrEmpty(context.HttpProtocolVersion))
        {
            activity.SetTag(SemanticConventions.Network.ProtocolVersion, context.HttpProtocolVersion);
        }

        // Client info
        if (!string.IsNullOrEmpty(context.ClientAddress))
        {
            activity.SetTag(SemanticConventions.Client.Address, context.ClientAddress);
        }

        if (context.ClientPort.HasValue)
        {
            activity.SetTag(SemanticConventions.Client.Port, context.ClientPort.Value);
        }
    }

    /// <summary>
    /// Sanitizes a resource URI for use in a span name (to keep cardinality low).
    /// </summary>
    private static string SanitizeResourceUri(string uri)
    {
        // For span names, we want low cardinality
        // Remove query strings and fragments, keep scheme + path
        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            return $"{parsed.Scheme}://{parsed.Host}{parsed.AbsolutePath}";
        }

        // If not a valid URI, truncate if too long
        const int maxLength = 50;
        return uri.Length > maxLength ? uri[..maxLength] + "..." : uri;
    }
}
