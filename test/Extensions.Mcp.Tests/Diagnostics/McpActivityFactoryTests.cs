// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests.Diagnostics;

public class McpActivityFactoryTests : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly List<Activity> _recordedActivities = new();

    public McpActivityFactoryTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == SemanticConventions.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _recordedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        foreach (var activity in _recordedActivities)
        {
            activity.Dispose();
        }
        _listener.Dispose();
    }

    [Fact]
    public void CreateToolActivity_CreatesActivity_WithCorrectSpanName()
    {
        var factory = new McpActivityFactory();

        using var activity = factory.CreateToolActivity("my-tool", null, null);

        Assert.NotNull(activity);
        Assert.Equal("tools/call my-tool", activity.DisplayName);
    }

    [Fact]
    public void CreateToolActivity_SetsRequiredAttributes()
    {
        var factory = new McpActivityFactory();

        using var activity = factory.CreateToolActivity("test-tool", null, null);

        Assert.NotNull(activity);
        Assert.Equal(SemanticConventions.Methods.ToolsCall, GetTag(activity, SemanticConventions.Mcp.MethodName));
        Assert.Equal("test-tool", GetTag(activity, SemanticConventions.GenAi.ToolName));
    }

    [Fact]
    public void CreateToolActivity_SetsRecommendedAttributes()
    {
        var factory = new McpActivityFactory();

        using var activity = factory.CreateToolActivity("test-tool", null, null);

        Assert.NotNull(activity);
        Assert.Equal(SemanticConventions.Operations.ExecuteTool, GetTag(activity, SemanticConventions.GenAi.OperationName));
        Assert.Equal(SemanticConventions.JsonRpc.Version, GetTag(activity, SemanticConventions.JsonRpc.ProtocolVersion));
        Assert.Equal(SemanticConventions.Network.ProtocolHttp, GetTag(activity, SemanticConventions.Network.ProtocolName));
        Assert.Equal(SemanticConventions.Network.TransportTcp, GetTag(activity, SemanticConventions.Network.Transport));
    }

    [Fact]
    public void CreateToolActivity_SetsSessionId_WhenProvided()
    {
        var factory = new McpActivityFactory();
        var context = new McpRequestTraceContext { SessionId = "session-abc-123" };

        using var activity = factory.CreateToolActivity("test-tool", null, null, context);

        Assert.NotNull(activity);
        Assert.Equal("session-abc-123", GetTag(activity, SemanticConventions.Mcp.SessionId));
    }

    [Fact]
    public void CreateToolActivity_DoesNotSetSessionId_WhenNull()
    {
        var factory = new McpActivityFactory();
        var context = new McpRequestTraceContext { SessionId = null };

        using var activity = factory.CreateToolActivity("test-tool", null, null, context);

        Assert.NotNull(activity);
        Assert.Null(GetTag(activity, SemanticConventions.Mcp.SessionId));
    }

    [Fact]
    public void CreateToolActivity_SetsProtocolVersion_WhenProvided()
    {
        var factory = new McpActivityFactory();
        var context = new McpRequestTraceContext { McpProtocolVersion = "2024-11-05" };

        using var activity = factory.CreateToolActivity("test-tool", null, null, context);

        Assert.NotNull(activity);
        Assert.Equal("2024-11-05", GetTag(activity, SemanticConventions.Mcp.ProtocolVersion));
    }

    [Fact]
    public void CreateToolActivity_SetsClientInfo_WhenProvided()
    {
        var factory = new McpActivityFactory();
        var context = new McpRequestTraceContext
        {
            ClientAddress = "192.168.1.100",
            ClientPort = 54321
        };

        using var activity = factory.CreateToolActivity("test-tool", null, null, context);

        Assert.NotNull(activity);
        Assert.Equal("192.168.1.100", GetTag(activity, SemanticConventions.Client.Address));
        Assert.Equal(54321, GetTagInt(activity, SemanticConventions.Client.Port));
    }

    [Fact]
    public void CreateToolActivity_SetsHttpProtocolVersion_WhenProvided()
    {
        var factory = new McpActivityFactory();
        var context = new McpRequestTraceContext { HttpProtocolVersion = "2" };

        using var activity = factory.CreateToolActivity("test-tool", null, null, context);

        Assert.NotNull(activity);
        Assert.Equal("2", GetTag(activity, SemanticConventions.Network.ProtocolVersion));
    }

    [Fact]
    public void CreateToolActivity_ExtractsParentContext_FromRequestParams()
    {
        var factory = new McpActivityFactory();
        var meta = new JsonObject
        {
            ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"
        };
        var requestParams = new CallToolRequestParams
        {
            Name = "test-tool",
            Meta = meta
        };

        using var activity = factory.CreateToolActivity("test-tool", requestParams, null);

        Assert.NotNull(activity);
        Assert.Equal("0af7651916cd43dd8448eb211c80319c", activity.TraceId.ToString());
        Assert.Equal("b7ad6b7169203331", activity.ParentSpanId.ToString());
    }

    [Fact]
    public void CreateToolActivity_LinksToTransportContext_WhenProvided()
    {
        var factory = new McpActivityFactory();
        var transportTraceId = ActivityTraceId.CreateRandom();
        var transportSpanId = ActivitySpanId.CreateRandom();
        var transportContext = new ActivityContext(transportTraceId, transportSpanId, ActivityTraceFlags.Recorded);

        using var activity = factory.CreateToolActivity("test-tool", null, transportContext);

        Assert.NotNull(activity);
        Assert.Single(activity.Links);
        Assert.Equal(transportTraceId, activity.Links.First().Context.TraceId);
    }

    [Fact]
    public void CreateResourceActivity_CreatesActivity_WithCorrectSpanName()
    {
        var factory = new McpActivityFactory();

        using var activity = factory.CreateResourceActivity("file:///path/to/resource", null, null);

        Assert.NotNull(activity);
        Assert.Contains("resources/read", activity.DisplayName);
    }

    [Fact]
    public void CreateResourceActivity_SetsRequiredAttributes()
    {
        var factory = new McpActivityFactory();

        using var activity = factory.CreateResourceActivity("file:///path/to/resource.txt", null, null);

        Assert.NotNull(activity);
        Assert.Equal(SemanticConventions.Methods.ResourcesRead, GetTag(activity, SemanticConventions.Mcp.MethodName));
        Assert.Equal("file:///path/to/resource.txt", GetTag(activity, SemanticConventions.Mcp.ResourceUri));
    }

    [Fact]
    public void CreateResourceActivity_SetsRecommendedAttributes()
    {
        var factory = new McpActivityFactory();

        using var activity = factory.CreateResourceActivity("file:///path", null, null);

        Assert.NotNull(activity);
        Assert.Equal(SemanticConventions.JsonRpc.Version, GetTag(activity, SemanticConventions.JsonRpc.ProtocolVersion));
        Assert.Equal(SemanticConventions.Network.ProtocolHttp, GetTag(activity, SemanticConventions.Network.ProtocolName));
        Assert.Equal(SemanticConventions.Network.TransportTcp, GetTag(activity, SemanticConventions.Network.Transport));
    }

    [Fact]
    public void CreateResourceActivity_SetsSessionId_WhenProvided()
    {
        var factory = new McpActivityFactory();
        var context = new McpRequestTraceContext { SessionId = "resource-session-456" };

        using var activity = factory.CreateResourceActivity("file:///path", null, null, context);

        Assert.NotNull(activity);
        Assert.Equal("resource-session-456", GetTag(activity, SemanticConventions.Mcp.SessionId));
    }

    [Fact]
    public void CreateResourceActivity_SetsClientInfo_WhenProvided()
    {
        var factory = new McpActivityFactory();
        var context = new McpRequestTraceContext
        {
            ClientAddress = "10.0.0.1",
            ClientPort = 8080
        };

        using var activity = factory.CreateResourceActivity("file:///path", null, null, context);

        Assert.NotNull(activity);
        Assert.Equal("10.0.0.1", GetTag(activity, SemanticConventions.Client.Address));
        Assert.Equal(8080, GetTagInt(activity, SemanticConventions.Client.Port));
    }

    [Fact]
    public void CreateResourceActivity_SanitizesUri_ForSpanName()
    {
        var factory = new McpActivityFactory();

        // URI with query string should be sanitized in span name
        using var activity = factory.CreateResourceActivity("https://example.com/path/to/resource?query=value&secret=123", null, null);

        Assert.NotNull(activity);
        // Span name should not include query string (low cardinality)
        Assert.Contains("https://example.com/path/to/resource", activity.DisplayName);
        // But full URI should be in the tag
        Assert.Equal("https://example.com/path/to/resource?query=value&secret=123", GetTag(activity, SemanticConventions.Mcp.ResourceUri));
    }

    [Fact]
    public void CreateResourceActivity_HandlesLongUri_ForSpanName()
    {
        var factory = new McpActivityFactory();

        // Non-URI string that's very long should be truncated in span name
        var longPath = new string('a', 100);

        using var activity = factory.CreateResourceActivity(longPath, null, null);

        Assert.NotNull(activity);
        // Full path should be in the tag
        Assert.Equal(longPath, GetTag(activity, SemanticConventions.Mcp.ResourceUri));
    }

    [Fact]
    public void CreateResourceActivity_ExtractsParentContext_FromRequestParams()
    {
        var factory = new McpActivityFactory();
        var meta = new JsonObject
        {
            ["traceparent"] = "00-1234567890abcdef1234567890abcdef-1234567890abcdef-01"
        };
        var requestParams = new ReadResourceRequestParams
        {
            Uri = "file:///path",
            Meta = meta
        };

        using var activity = factory.CreateResourceActivity("file:///path", requestParams, null);

        Assert.NotNull(activity);
        Assert.Equal("1234567890abcdef1234567890abcdef", activity.TraceId.ToString());
    }

    [Fact]
    public void CreateResourceActivity_LinksToTransportContext_WhenProvided()
    {
        var factory = new McpActivityFactory();
        var transportTraceId = ActivityTraceId.CreateRandom();
        var transportSpanId = ActivitySpanId.CreateRandom();
        var transportContext = new ActivityContext(transportTraceId, transportSpanId, ActivityTraceFlags.Recorded);

        using var activity = factory.CreateResourceActivity("file:///path", null, transportContext);

        Assert.NotNull(activity);
        Assert.Single(activity.Links);
        Assert.Equal(transportTraceId, activity.Links.First().Context.TraceId);
    }

    private static string? GetTag(Activity activity, string key)
    {
        return activity.Tags.FirstOrDefault(t => t.Key == key).Value;
    }

    private static int? GetTagInt(Activity activity, string key)
    {
        var obj = activity.TagObjects.FirstOrDefault(t => t.Key == key).Value;
        return obj as int?;
    }
}
