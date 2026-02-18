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

    [Fact]
    public void SetJsonRpcError_ThrowsArgumentNullException_WhenActivityIsNull()
    {
        Activity? activity = null;

        Assert.Throws<ArgumentNullException>(() => activity!.SetJsonRpcError(-32600, "Invalid Request"));
    }

    [Fact]
    public void SetJsonRpcError_SetsErrorTypeToErrorCode()
    {
        using var activity = _activitySource.StartActivity("test");
        Assert.NotNull(activity);

        activity.SetJsonRpcError(-32600, "Invalid Request");

        var errorType = activity.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Error.Type);
        Assert.Equal("-32600", errorType.Value);
    }

    [Fact]
    public void SetJsonRpcError_SetsRpcResponseStatusCode()
    {
        using var activity = _activitySource.StartActivity("test");
        Assert.NotNull(activity);

        activity.SetJsonRpcError(-32601, "Method not found");

        var statusCode = activity.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Rpc.ResponseStatusCode);
        Assert.Equal("-32601", statusCode.Value);
    }

    [Fact]
    public void SetJsonRpcError_SetsActivityStatusToError()
    {
        using var activity = _activitySource.StartActivity("test");
        Assert.NotNull(activity);

        activity.SetJsonRpcError(-32700, "Parse error");

        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Parse error", activity.StatusDescription);
    }

    [Fact]
    public void SetJsonRpcError_UsesDefaultMessage_WhenErrorMessageIsNull()
    {
        using var activity = _activitySource.StartActivity("test");
        Assert.NotNull(activity);

        activity.SetJsonRpcError(-32600, null);

        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("JSON-RPC error: -32600", activity.StatusDescription);
    }

    [Fact]
    public void SetJsonRpcError_ReturnsActivityForChaining()
    {
        using var activity = _activitySource.StartActivity("test");
        Assert.NotNull(activity);

        var result = activity.SetJsonRpcError(-32600, "error");

        Assert.Same(activity, result);
    }

    [Fact]
    public void SetToolError_ThrowsArgumentNullException_WhenActivityIsNull()
    {
        Activity? activity = null;

        Assert.Throws<ArgumentNullException>(() => activity!.SetToolError("error"));
    }

    [Fact]
    public void SetToolError_SetsErrorTypeToToolError()
    {
        using var activity = _activitySource.StartActivity("test");
        Assert.NotNull(activity);

        activity.SetToolError();

        var errorType = activity.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Error.Type);
        Assert.Equal(SemanticConventions.Error.ToolError, errorType.Value);
    }

    [Fact]
    public void SetToolError_SetsActivityStatusToError()
    {
        using var activity = _activitySource.StartActivity("test");
        Assert.NotNull(activity);

        activity.SetToolError("Tool failed to execute");

        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Tool failed to execute", activity.StatusDescription);
    }

    [Fact]
    public void SetToolError_UsesDefaultMessage_WhenErrorMessageIsNull()
    {
        using var activity = _activitySource.StartActivity("test");
        Assert.NotNull(activity);

        activity.SetToolError();

        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Tool execution returned an error", activity.StatusDescription);
    }

    [Fact]
    public void SetToolError_ReturnsActivityForChaining()
    {
        using var activity = _activitySource.StartActivity("test");
        Assert.NotNull(activity);

        var result = activity.SetToolError();

        Assert.Same(activity, result);
    }
}
