// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests.Diagnostics;

public class ActivityExtensionsTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _listener;

    public ActivityExtensionsTests()
    {
        _activitySource = new ActivitySource("Test.ActivityExtensions");
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        _activitySource.Dispose();
    }

    [Fact]
    public void SetExceptionStatus_ThrowsArgumentNullException_WhenActivityIsNull()
    {
        Activity? activity = null;

        Assert.Throws<ArgumentNullException>(() => activity!.SetExceptionStatus(new Exception()));
    }

    [Fact]
    public void SetExceptionStatus_ReturnsActivity_WhenExceptionIsNull()
    {
        using var activity = _activitySource.StartActivity("test");
        Assert.NotNull(activity);

        var result = activity.SetExceptionStatus(null);

        Assert.Same(activity, result);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
    }

    [Fact]
    public void SetExceptionStatus_SetsErrorTypeTag()
    {
        using var activity = _activitySource.StartActivity("test");
        Assert.NotNull(activity);

        var exception = new InvalidOperationException("Test error");
        activity.SetExceptionStatus(exception);

        var errorType = activity.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Error.Type);
        Assert.Equal(typeof(InvalidOperationException).FullName, errorType.Value);
    }

    [Fact]
    public void SetExceptionStatus_SetsActivityStatusToError()
    {
        using var activity = _activitySource.StartActivity("test");
        Assert.NotNull(activity);

        var exception = new InvalidOperationException("Test error message");
        activity.SetExceptionStatus(exception);

        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Test error message", activity.StatusDescription);
    }

    [Fact]
    public void SetExceptionStatus_AddsExceptionEvent()
    {
        using var activity = _activitySource.StartActivity("test");
        Assert.NotNull(activity);

        var exception = new InvalidOperationException("Test error");
        activity.SetExceptionStatus(exception);

        var exceptionEvent = activity.Events.FirstOrDefault(e => e.Name == "exception");
        Assert.NotEqual(default, exceptionEvent);
    }

    [Fact]
    public void SetExceptionStatus_ReturnsActivityForChaining()
    {
        using var activity = _activitySource.StartActivity("test");
        Assert.NotNull(activity);

        var result = activity.SetExceptionStatus(new Exception("error"));

        Assert.Same(activity, result);
    }

}
