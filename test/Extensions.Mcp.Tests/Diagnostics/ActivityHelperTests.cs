// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests.Diagnostics;

public class ActivityHelperTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _listener;

    public ActivityHelperTests()
    {
        _activitySource = new ActivitySource("Test.ActivityHelper");
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Test.ActivityHelper",
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
    public void Constructor_ThrowsArgumentNullException_WhenSourceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ActivityHelper(null!));
    }

    [Fact]
    public void StartServerActivity_CreatesActivity_WithCorrectName()
    {
        var helper = new ActivityHelper(_activitySource);

        using var activity = helper.StartServerActivity("test-operation", null, null);

        Assert.NotNull(activity);
        Assert.Equal("test-operation", activity.DisplayName);
    }

    [Fact]
    public void StartServerActivity_CreatesActivity_WithServerKind()
    {
        var helper = new ActivityHelper(_activitySource);

        using var activity = helper.StartServerActivity("test-operation", null, null);

        Assert.NotNull(activity);
        Assert.Equal(ActivityKind.Server, activity.Kind);
    }

    [Fact]
    public void StartServerActivity_UsesParentContext_WhenProvided()
    {
        var helper = new ActivityHelper(_activitySource);
        var parentTraceId = ActivityTraceId.CreateRandom();
        var parentSpanId = ActivitySpanId.CreateRandom();
        var parentContext = new ActivityContext(parentTraceId, parentSpanId, ActivityTraceFlags.Recorded);

        using var activity = helper.StartServerActivity("test-operation", parentContext, null);

        Assert.NotNull(activity);
        Assert.Equal(parentTraceId, activity.TraceId);
        Assert.Equal(parentSpanId, activity.ParentSpanId);
    }

    [Fact]
    public void StartServerActivity_LinksToTransportContext_WhenProvided()
    {
        var helper = new ActivityHelper(_activitySource);
        var transportTraceId = ActivityTraceId.CreateRandom();
        var transportSpanId = ActivitySpanId.CreateRandom();
        var transportContext = new ActivityContext(transportTraceId, transportSpanId, ActivityTraceFlags.Recorded);

        using var activity = helper.StartServerActivity("test-operation", null, transportContext);

        Assert.NotNull(activity);
        Assert.Single(activity.Links);
        Assert.Equal(transportTraceId, activity.Links.First().Context.TraceId);
        Assert.Equal(transportSpanId, activity.Links.First().Context.SpanId);
    }

    [Fact]
    public void StartServerActivity_LinksToTransportContext_WhenBothContextsProvided()
    {
        var helper = new ActivityHelper(_activitySource);

        var parentTraceId = ActivityTraceId.CreateRandom();
        var parentSpanId = ActivitySpanId.CreateRandom();
        var parentContext = new ActivityContext(parentTraceId, parentSpanId, ActivityTraceFlags.Recorded);

        var transportTraceId = ActivityTraceId.CreateRandom();
        var transportSpanId = ActivitySpanId.CreateRandom();
        var transportContext = new ActivityContext(transportTraceId, transportSpanId, ActivityTraceFlags.Recorded);

        using var activity = helper.StartServerActivity("test-operation", parentContext, transportContext);

        Assert.NotNull(activity);
        // Should use parent context for parent, and link to transport
        Assert.Equal(parentTraceId, activity.TraceId);
        Assert.Single(activity.Links);
        Assert.Equal(transportTraceId, activity.Links.First().Context.TraceId);
    }

    [Fact]
    public void StartServerActivity_InvokesConfigureCallback()
    {
        var helper = new ActivityHelper(_activitySource);
        var callbackInvoked = false;

        using var activity = helper.StartServerActivity("test-operation", null, null, a =>
        {
            callbackInvoked = true;
            a.SetTag("custom.tag", "value");
        });

        Assert.NotNull(activity);
        Assert.True(callbackInvoked);
        var tag = activity.Tags.FirstOrDefault(t => t.Key == "custom.tag");
        Assert.Equal("value", tag.Value);
    }

    [Fact]
    public void CaptureCurrentContext_ReturnsNull_WhenNoCurrentActivity()
    {
        // Ensure no current activity
        Activity.Current = null;

        var result = ActivityHelper.CaptureCurrentContext();

        Assert.Null(result);
    }

    [Fact]
    public void CaptureCurrentContext_ReturnsCurrentActivityContext()
    {
        using var activity = _activitySource.StartActivity("parent-activity");
        Assert.NotNull(activity);

        var result = ActivityHelper.CaptureCurrentContext();

        Assert.NotNull(result);
        Assert.Equal(activity.TraceId, result.Value.TraceId);
        Assert.Equal(activity.SpanId, result.Value.SpanId);
    }
}
