// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

/// <summary>
/// Central instrumentation class for creating and managing Activity instances for MCP operations,
/// following OpenTelemetry semantic conventions.
/// </summary>
internal static class McpInstrumentation
{
    private static readonly ActivitySource _source = new(
        McpDiagnosticsConstants.ActivitySourceName,
        McpDiagnosticsConstants.ActivitySourceVersion);

    /// <summary>
    /// Captures the current ambient activity context (typically the transport/HTTP span).
    /// Call this before creating a new activity to preserve the link.
    /// </summary>
    public static ActivityContext? CaptureCurrentContext()
        => Activity.Current?.Context;

    /// <summary>
    /// Creates an activity for an MCP session initialization operation.
    /// </summary>
    /// <param name="transportContext">The transport span context to link to.</param>
    /// <param name="requestContext">The request context with client and protocol info.</param>
    /// <returns>The created McpActivityScope.</returns>
    public static McpActivityScope CreateSessionActivity(
        ActivityContext? transportContext,
        McpRequestTraceContext requestContext = default)
        => StartServerActivity(
            SemanticConventions.Methods.Initialize,
            parentContext: null,
            transportContext,
            a => ConfigureSessionActivity(a, requestContext));

    /// <summary>
    /// Creates an activity for an MCP session termination — either an explicit DELETE request
    /// (streamable HTTP) or an implicit SSE connection close.
    /// </summary>
    /// <param name="transportContext">The transport span context to link to.</param>
    /// <param name="requestContext">The request context with session and client info.</param>
    /// <returns>The created McpActivityScope.</returns>
    public static McpActivityScope CreateSessionEndActivity(
        ActivityContext? transportContext,
        McpRequestTraceContext requestContext = default)
        => StartServerActivity(
            SemanticConventions.Methods.SessionDelete,
            parentContext: null,
            transportContext,
            a => ConfigureSessionEndActivity(a, requestContext));

    /// <summary>
    /// Records a session-end span without throwing. Safe to call from exception handlers
    /// where a telemetry failure must not replace or suppress the original control flow.
    /// </summary>
    public static void RecordSessionEnd(ActivityContext? transportContext, McpRequestTraceContext requestContext = default)
    {
        try
        {
            using var scope = CreateSessionEndActivity(transportContext, requestContext);
        }
        catch { }
    }

    private static McpActivityScope StartServerActivity(
        string name,
        ActivityContext? parentContext,
        ActivityContext? transportContext,
        Action<Activity>? configure = null)
    {
        ActivityLink[]? links = transportContext.HasValue
            ? [new ActivityLink(transportContext.Value)]
            : null;

        // Save and clear Activity.Current before starting the MCP activity.
        // This prevents StartActivity from automatically adopting the transport
        // span as parent (when no explicit MCP parent is provided).
        // The transport relationship is captured via a link instead, per MCP semantic conventions.
        // After creation, Activity.Current = the new tool/resource activity so customer code
        // runs inside this span. McpActivityScope.Dispose() stops the activity and restores
        // Activity.Current to previous.
        var previous = Activity.Current;
        Activity.Current = null;

        var activity = parentContext.HasValue
            ? _source.StartActivity(name, ActivityKind.Server, parentContext.Value, links: links)
            : _source.StartActivity(name, ActivityKind.Server, parentContext: default, links: links);

        if (activity is not null)
        {
            try
            {
                configure?.Invoke(activity);
                // Leave Activity.Current = activity; McpActivityScope.Dispose() handles cleanup.
            }
            catch
            {
                // Telemetry configuration failed — discard the span silently.
                // A diagnostic failure must never affect request handling.
                activity.Dispose();
                Activity.Current = previous;
                return new McpActivityScope(null, previous);
            }
        }
        else
        {
            // No activity created (no listeners); undo the null we set above.
            Activity.Current = previous;
        }

        return new McpActivityScope(activity, previous);
    }

    private static void ConfigureSessionActivity(Activity activity, McpRequestTraceContext context)
    {
        activity.SetTag(SemanticConventions.Mcp.MethodName, SemanticConventions.Methods.Initialize);
        ApplyTransportDefaults(activity);
        ApplyRequestContext(activity, context);
    }

    private static void ConfigureSessionEndActivity(Activity activity, McpRequestTraceContext context)
    {
        activity.SetTag(SemanticConventions.Mcp.MethodName, SemanticConventions.Methods.SessionDelete);
        ApplyTransportDefaults(activity);
        ApplyRequestContext(activity, context);
    }

    /// <summary>Sets the jsonrpc and network tags that apply to every MCP server span.</summary>
    private static void ApplyTransportDefaults(Activity activity)
    {
        activity.SetTag(SemanticConventions.JsonRpc.ProtocolVersion, SemanticConventions.JsonRpc.Version);
        activity.SetTag(SemanticConventions.Network.ProtocolName, SemanticConventions.Network.ProtocolHttp);
        activity.SetTag(SemanticConventions.Network.Transport, SemanticConventions.Network.TransportTcp);
    }

    /// <summary>Sets the optional per-request context tags (session, protocol version, client address/port).</summary>
    private static void ApplyRequestContext(Activity activity, McpRequestTraceContext context)
    {
        if (!string.IsNullOrEmpty(context.SessionId))
        {
            activity.SetTag(SemanticConventions.Mcp.SessionId, context.SessionId);
        }

        if (!string.IsNullOrEmpty(context.McpProtocolVersion))
        {
            activity.SetTag(SemanticConventions.Mcp.ProtocolVersion, context.McpProtocolVersion);
        }

        if (!string.IsNullOrEmpty(context.HttpProtocolVersion))
        {
            activity.SetTag(SemanticConventions.Network.ProtocolVersion, context.HttpProtocolVersion);
        }

        if (!string.IsNullOrEmpty(context.ClientAddress))
        {
            activity.SetTag(SemanticConventions.Client.Address, context.ClientAddress);
        }

        if (context.ClientPort.HasValue)
        {
            activity.SetTag(SemanticConventions.Client.Port, context.ClientPort.Value);
        }
    }

}

/// <summary>
/// Encapsulates an MCP activity span and the ambient context to restore on disposal.
/// Disposing stops the tool/resource activity and restores Activity.Current to the
/// transport activity that was ambient before the span was created.
/// </summary>
internal readonly struct McpActivityScope : IDisposable
{
    private readonly Activity? _previous;

    internal McpActivityScope(Activity? activity, Activity? previous)
    {
        Activity = activity;
        _previous = previous;
    }

    /// <summary>The MCP activity span, or null if no listeners are registered.</summary>
    internal Activity? Activity { get; }

    public void Dispose()
    {
        try
        {
            Activity?.Dispose();
        }
        finally
        {
            Activity.Current = _previous;
        }
    }
}
