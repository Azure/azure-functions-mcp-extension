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

        SetErrorStatus(activity, exception.GetType().FullName, exception.Message);

        // Add exception event if one hasn't already been recorded
        if (!activity.Events.Any(e => string.Equals(e.Name, SemanticConventions.Exception.EventName, StringComparison.Ordinal)))
        {
            var exceptionTags = new ActivityTagsCollection
            {
                { SemanticConventions.Exception.Message, exception.Message },
                { SemanticConventions.Exception.Stacktrace, exception.ToString() },
                { SemanticConventions.Exception.Type, exception.GetType().FullName }
            };

            activity.AddEvent(new ActivityEvent(SemanticConventions.Exception.EventName, DateTimeOffset.UtcNow, exceptionTags));
        }

        return activity;
    }

    private static void SetErrorStatus(Activity activity, string? errorType, string statusDescription)
    {
        activity.SetTag(SemanticConventions.Error.Type, errorType);
        activity.SetStatus(ActivityStatusCode.Error, statusDescription);
    }
}
