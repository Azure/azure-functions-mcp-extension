using System.Diagnostics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics
{
    internal static class ActivityExtensions
    {
        public static Activity RecordException(this Activity activity, Exception exception, DateTimeOffset timestamp = default)
        {
            if (activity is null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            if (exception is null)
            {
                return activity;
            }

            foreach (var item in activity.Events)
            {
                if (item.Name == TraceConstants.AttributeExceptionEventName)
                {
                    // Exception event already exists, no need to add it again.
                    return activity;
                }
            }

            var exceptionTags = new ActivityTagsCollection
            {
                { TraceConstants.AttributeExceptionMessage, exception.Message },
                { TraceConstants.AttributeExceptionStacktrace, exception.ToString() },
                { TraceConstants.AttributeExceptionType, exception.GetType().ToString() }
            };

            return activity.AddEvent(new ActivityEvent(TraceConstants.AttributeExceptionEventName, timestamp, exceptionTags));
        }
    }
}
