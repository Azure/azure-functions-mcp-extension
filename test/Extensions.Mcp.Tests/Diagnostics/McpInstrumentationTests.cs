// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests.Diagnostics;

public class McpInstrumentationTests : IDisposable
{
    private readonly ActivitySource _testSource;
    private readonly ActivityListener _listener;

    public McpInstrumentationTests()
    {
        _testSource = new ActivitySource("Test.McpInstrumentation");
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
        _testSource.Dispose();
    }

    // --- CreateSessionActivity ---

    [Fact]
    public void CreateSessionActivity_WhenAmbientActivityExists_CreatesLinkToIt()
    {
        using var parent = _testSource.StartActivity("parent");
        var parentContext = parent!.Context;

        using var scope = McpInstrumentation.CreateSessionActivity();

        Assert.NotNull(scope.ScopeActivity);
        Assert.Single(scope.ScopeActivity.Links);
        Assert.Equal(parentContext, scope.ScopeActivity.Links.First().Context);
    }

    [Fact]
    public void CreateSessionActivity_WhenNoAmbientActivity_CreatesNoLinks()
    {
        Activity.Current = null;

        using var scope = McpInstrumentation.CreateSessionActivity();

        Assert.NotNull(scope.ScopeActivity);
        Assert.Empty(scope.ScopeActivity.Links);
    }

    [Fact]
    public void CreateSessionActivity_SetsCurrent_ToNewActivity()
    {
        Activity.Current = null;

        using var scope = McpInstrumentation.CreateSessionActivity();

        Assert.Same(scope.ScopeActivity, Activity.Current);
    }

    // --- McpActivityScope.Dispose ---

    [Fact]
    public void Dispose_RestoresPreviousActivity()
    {
        using var parent = _testSource.StartActivity("parent");

        using (McpInstrumentation.CreateSessionActivity())
        {
            Assert.NotSame(parent, Activity.Current);
        }

        Assert.Same(parent, Activity.Current);
    }

    [Fact]
    public void Dispose_RestoresNullWhenNoPreviousActivity()
    {
        Activity.Current = null;

        using (McpInstrumentation.CreateSessionActivity()) { }

        Assert.Null(Activity.Current);
    }

    [Fact]
    public void Dispose_WhenActivityIsNull_StillRestoresPrevious()
    {
        // Covers both the configure-failure and no-listener paths — both return
        // McpActivityScope(null, previous) and must still restore Activity.Current.
        using var parent = _testSource.StartActivity("parent");
        Activity.Current = null; // mirrors what StartServerActivity does before returning

        var scope = new McpActivityScope(null, parent);
        scope.Dispose();

        Assert.Same(parent, Activity.Current);
    }

    // --- Concurrent scope isolation ---

    [Fact]
    public async Task ConcurrentScopes_OnSeparateTasks_DoNotInterfere()
    {
        // Activity.Current is [AsyncLocal] — each Task gets its own copy.
        // Two concurrent MCP scopes must neither see nor corrupt each other's current activity.
        var scope2Started = new TaskCompletionSource();
        var scope1CanFinish = new TaskCompletionSource();

        Activity? activity1 = null;
        Activity? activity2 = null;

        var task1 = Task.Run(async () =>
        {
            using var scope = McpInstrumentation.CreateSessionActivity();
            activity1 = scope.ScopeActivity;
            scope2Started.SetResult();
            await scope1CanFinish.Task;
            Assert.Same(activity1, Activity.Current);
        });

        var task2 = Task.Run(async () =>
        {
            await scope2Started.Task;
            using var scope = McpInstrumentation.CreateSessionActivity();
            activity2 = scope.ScopeActivity;
            scope1CanFinish.SetResult();
        });

        await Task.WhenAll(task1, task2);

        Assert.NotNull(activity1);
        Assert.NotNull(activity2);
        Assert.NotSame(activity1, activity2);
    }
}
