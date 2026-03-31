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
}
