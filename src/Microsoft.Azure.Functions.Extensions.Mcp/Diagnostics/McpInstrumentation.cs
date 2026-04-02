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
    /// Creates an activity for an MCP session initialization operation.
    /// </summary>
    /// <param name="requestContext">The request context with client and protocol info.</param>
    /// <returns>The created McpActivityScope.</returns>
    public static McpActivityScope CreateSessionActivity(
        McpRequestTraceContext requestContext = default)
        => StartServerActivity(
            SemanticConventions.Methods.Initialize,
            a => ConfigureActivity(a, requestContext));

    /// <summary>
    /// Creates an activity for an MCP session termination — either an explicit DELETE request
    /// (streamable HTTP) or an implicit SSE connection close.
    /// </summary>
    /// <param name="requestContext">The request context with session and client info.</param>
    /// <returns>The created McpActivityScope.</returns>
    public static McpActivityScope CreateSessionEndActivity(
        McpRequestTraceContext requestContext = default)
        => StartServerActivity(
            SemanticConventions.Methods.SessionDelete,
            a => ConfigureActivity(a, requestContext));

    /// <summary>
    /// Creates a session span, executes <paramref name="action"/>, and records any exception on
    /// the span before rethrowing. Centralises the instrumentation pattern shared by both transports.
    /// </summary>
    private static McpActivityScope StartServerActivity(
        string name,
        Action<Activity>? configure = null)
    {
        // Save and clear Activity.Current before starting the MCP activity.
        // This prevents StartActivity from automatically adopting the transport span as parent.
        // The transport relationship is captured via a link instead, per MCP semantic conventions.
        // After creation, Activity.Current = the new activity so customer code runs inside this span.
        // McpActivityScope.Dispose() stops the activity and restores Activity.Current to previous.
        var previous = Activity.Current;
        ActivityLink[]? links = previous is not null
            ? [new ActivityLink(previous.Context)]
            : null;
        Activity.Current = null;

        var activity = _source.StartActivity(name, ActivityKind.Server, parentContext: default, links: links);

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

    private static void ConfigureActivity(Activity activity, McpRequestTraceContext context)
    {
        // OperationName is the span name passed to StartActivity — same as the MCP method name.
        activity.SetTag(SemanticConventions.Mcp.MethodName, activity.OperationName);
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
        ScopeActivity = activity;
        _previous = previous;
    }

    /// <summary>The MCP activity span, or null if no listeners are registered.</summary>
    internal Activity? ScopeActivity { get; }

    public void Dispose()
    {
        try
        {
            ScopeActivity?.Dispose();
        }
        finally
        {
            Activity.Current = _previous;
        }
    }
}
