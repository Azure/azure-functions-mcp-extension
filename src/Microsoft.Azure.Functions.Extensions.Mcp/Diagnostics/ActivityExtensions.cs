// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

internal static class ActivityExtensions
{
    public static Activity SetExceptionStatus(this Activity activity, Exception? exception, DateTimeOffset timestamp = default)
    {        
        ArgumentNullException.ThrowIfNull(activity);

        if (exception is null)
        {
            return activity;
        }

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);

        if (activity.Events.Any(e => string.Equals(e.Name, TraceConstants.ExceptionEventNameAttribute, StringComparison.Ordinal)))
        {
            return activity;
        }

        var exceptionTags = new ActivityTagsCollection
        {
            { TraceConstants.ExceptionMessageAttribute, exception.Message },
            { TraceConstants.ExceptionStacktraceAttribute, exception.ToString() },
            { TraceConstants.ExceptionTypeAttribute, exception.GetType().ToString() }
        };

        return activity.AddEvent(new ActivityEvent(TraceConstants.ExceptionEventNameAttribute, timestamp, exceptionTags));
    }
}

