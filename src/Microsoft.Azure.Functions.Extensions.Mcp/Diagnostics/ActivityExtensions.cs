// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

/// <summary>
/// Extension methods for Activity to support MCP semantic conventions.
/// </summary>
internal static class ActivityExtensions
{
    /// <summary>
    /// Sets the activity status to Error and records exception details.
    /// </summary>
    /// <param name="activity">The activity to update.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>The activity for chaining.</returns>
    public static Activity SetExceptionStatus(this Activity activity, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (exception is null)
        {
            return activity;
        }

        // Set error.type attribute per semantic conventions
        activity.SetTag(SemanticConventions.Error.Type, exception.GetType().FullName);

        // Set activity status to Error
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);

        // Add exception event if one hasn't already been recorded
        if (!activity.Events.Any(e => string.Equals(e.Name, SemanticConventions.Exception.EventName, StringComparison.Ordinal)))
        {
            var exceptionTags = new ActivityTagsCollection
            {
                { SemanticConventions.Exception.Message, exception.Message },
                { SemanticConventions.Exception.Stacktrace, exception.ToString() },
                { SemanticConventions.Exception.Type, exception.GetType().ToString() }
            };

            activity.AddEvent(new ActivityEvent(SemanticConventions.Exception.EventName, DateTimeOffset.UtcNow, exceptionTags));
        }

        return activity;
    }

    /// <summary>
    /// Sets the activity status for a JSON-RPC error response.
    /// </summary>
    /// <param name="activity">The activity to update.</param>
    /// <param name="errorCode">The JSON-RPC error code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>The activity for chaining.</returns>
    public static Activity SetJsonRpcError(this Activity activity, int errorCode, string? errorMessage)
    {
        ArgumentNullException.ThrowIfNull(activity);

        // Set error.type to the string representation of the error code
        activity.SetTag(SemanticConventions.Error.Type, errorCode.ToString());

        // Set rpc.response.status_code
        activity.SetTag(SemanticConventions.Rpc.ResponseStatusCode, errorCode.ToString());

        // Set activity status to Error
        activity.SetStatus(ActivityStatusCode.Error, errorMessage ?? $"JSON-RPC error: {errorCode}");

        return activity;
    }

    /// <summary>
    /// Sets the activity status for a tool error (when CallToolResult.IsError is true).
    /// </summary>
    /// <param name="activity">The activity to update.</param>
    /// <param name="errorMessage">Optional error message from the tool result.</param>
    /// <returns>The activity for chaining.</returns>
    public static Activity SetToolError(this Activity activity, string? errorMessage = null)
    {
        ArgumentNullException.ThrowIfNull(activity);

        // Per semantic conventions: when CallToolResult has isError=true, set error.type to "tool_error"
        activity.SetTag(SemanticConventions.Error.Type, SemanticConventions.Error.ToolError);

        // Set activity status to Error
        activity.SetStatus(ActivityStatusCode.Error, errorMessage ?? "Tool execution returned an error");

        return activity;
    }
}
