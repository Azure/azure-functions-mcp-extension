// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

/// <summary>
/// Helper class for creating and managing Activity instances for MCP operations.
/// </summary>
internal sealed class ActivityHelper
{
    private readonly ActivitySource _source;

    public ActivityHelper(ActivitySource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary>
    /// Starts a new server activity with the specified parameters.
    /// </summary>
    /// <param name="name">The span name (should follow semantic convention: "{method} {target}").</param>
    /// <param name="parentContext">The parent context extracted from params._meta, if available.</param>
    /// <param name="transportContext">The ambient transport context to link to, if available.</param>
    /// <param name="configure">Optional callback to configure additional tags.</param>
    /// <returns>The created Activity, or null if no listeners are registered.</returns>
    public Activity? StartServerActivity(
        string name,
        ActivityContext? parentContext,
        ActivityContext? transportContext,
        Action<Activity>? configure = null)
    {
        // Build links if transport context is available
        ActivityLink[]? links = transportContext.HasValue
            ? [new ActivityLink(transportContext.Value)]
            : null;

        Activity? activity;

        if (parentContext.HasValue)
        {
            // Use the MCP client context as parent
            activity = _source.StartActivity(name, ActivityKind.Server, parentContext.Value, links: links);
        }
        else
        {
            // No MCP parent context — start as a root span.
            // Clear Activity.Current to prevent StartActivity from automatically
            // adopting the transport span as parent (default(ActivityContext) causes
            // .NET to fall back to Activity.Current). The transport relationship is
            // captured via the link instead, per MCP semantic conventions.
            var previous = Activity.Current;
            Activity.Current = null;

            activity = _source.StartActivity(name, ActivityKind.Server, parentContext: default, links: links);

            // Restore the transport activity as Activity.Current. The MCP activity
            // is intentionally not the ambient current — it is managed explicitly by
            // the caller. This ensures the transport activity stack is not disrupted
            // when the MCP activity stops (Activity.Stop sets Activity.Current to
            // Parent, which would be null and orphan the transport activity).
            Activity.Current = previous;
        }

        if (activity is not null)
        {
            configure?.Invoke(activity);
        }

        return activity;
    }

    /// <summary>
    /// Captures the current ambient activity context (typically the transport/HTTP span).
    /// Call this before creating a new activity to preserve the link.
    /// </summary>
    /// <returns>The current activity context, or null if no activity is current.</returns>
    public static ActivityContext? CaptureCurrentContext()
    {
        return Activity.Current?.Context;
    }
}
