// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

internal sealed class ActivityHelper
{
    private readonly ActivitySource _source;

    public ActivityHelper(ActivitySource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public Activity? StartServerActivity(string name, object? context, ActivityContext rootContext, Action<Activity>? configure = null)
    {
        var activity = _source.StartActivity(name, ActivityKind.Server, rootContext);

        if (activity is not null)
        {
            configure?.Invoke(activity);
        }

        return activity;
    }
}
