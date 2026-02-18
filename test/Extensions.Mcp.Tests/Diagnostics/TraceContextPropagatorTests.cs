// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests.Diagnostics;

public class TraceContextPropagatorTests
{
    [Fact]
    public void Extract_ReturnsNull_WhenRequestParamsIsNull()
    {
        var result = TraceContextPropagator.Extract(null);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_ReturnsNull_WhenMetaIsNull()
    {
        var requestParams = new CallToolRequestParams
        {
            Name = "test",
            Meta = null
        };

        var result = TraceContextPropagator.Extract(requestParams);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_ReturnsNull_WhenTraceparentNotPresent()
    {
        var meta = new JsonObject { ["other"] = "value" };
        var requestParams = new CallToolRequestParams
        {
            Name = "test",
            Meta = meta
        };

        var result = TraceContextPropagator.Extract(requestParams);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_ReturnsNull_WhenTraceparentIsEmpty()
    {
        var meta = new JsonObject { ["traceparent"] = "" };
        var requestParams = new CallToolRequestParams
        {
            Name = "test",
            Meta = meta
        };

        var result = TraceContextPropagator.Extract(requestParams);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_ReturnsNull_WhenTraceparentIsInvalidFormat()
    {
        var meta = new JsonObject { ["traceparent"] = "invalid-format" };
        var requestParams = new CallToolRequestParams
        {
            Name = "test",
            Meta = meta
        };

        var result = TraceContextPropagator.Extract(requestParams);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_ReturnsNull_WhenVersionIsFF()
    {
        var meta = new JsonObject { ["traceparent"] = "ff-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" };
        var requestParams = new CallToolRequestParams
        {
            Name = "test",
            Meta = meta
        };

        var result = TraceContextPropagator.Extract(requestParams);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_ReturnsNull_WhenTraceIdIsAllZeros()
    {
        var meta = new JsonObject { ["traceparent"] = "00-00000000000000000000000000000000-b7ad6b7169203331-01" };
        var requestParams = new CallToolRequestParams
        {
            Name = "test",
            Meta = meta
        };

        var result = TraceContextPropagator.Extract(requestParams);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_ReturnsNull_WhenSpanIdIsAllZeros()
    {
        var meta = new JsonObject { ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-0000000000000000-01" };
        var requestParams = new CallToolRequestParams
        {
            Name = "test",
            Meta = meta
        };

        var result = TraceContextPropagator.Extract(requestParams);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_ReturnsValidContext_WhenTraceparentIsValid()
    {
        var meta = new JsonObject { ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" };
        var requestParams = new CallToolRequestParams
        {
            Name = "test",
            Meta = meta
        };

        var result = TraceContextPropagator.Extract(requestParams);

        Assert.NotNull(result);
        Assert.Equal("0af7651916cd43dd8448eb211c80319c", result.Value.TraceId.ToString());
        Assert.Equal("b7ad6b7169203331", result.Value.SpanId.ToString());
        Assert.Equal(ActivityTraceFlags.Recorded, result.Value.TraceFlags);
    }

    [Fact]
    public void Extract_ReturnsValidContext_WithTracestate()
    {
        var meta = new JsonObject
        {
            ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
            ["tracestate"] = "congo=t61rcWkgMzE"
        };
        var requestParams = new CallToolRequestParams
        {
            Name = "test",
            Meta = meta
        };

        var result = TraceContextPropagator.Extract(requestParams);

        Assert.NotNull(result);
        Assert.Equal("congo=t61rcWkgMzE", result.Value.TraceState);
    }

    [Fact]
    public void Extract_ReturnsValidContext_WithUnrecordedTraceFlags()
    {
        var meta = new JsonObject { ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00" };
        var requestParams = new CallToolRequestParams
        {
            Name = "test",
            Meta = meta
        };

        var result = TraceContextPropagator.Extract(requestParams);

        Assert.NotNull(result);
        Assert.Equal(ActivityTraceFlags.None, result.Value.TraceFlags);
    }

    [Fact]
    public void Extract_ReturnsNull_WhenTraceparentHasUppercaseHex()
    {
        // W3C spec requires lowercase hex for traceparent
        // ActivityContext.TryParse rejects uppercase hex gracefully
        var meta = new JsonObject { ["traceparent"] = "00-0AF7651916CD43DD8448EB211C80319C-B7AD6B7169203331-01" };
        var requestParams = new CallToolRequestParams
        {
            Name = "test",
            Meta = meta
        };

        var result = TraceContextPropagator.Extract(requestParams);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_WorksWithReadResourceRequestParams()
    {
        var meta = new JsonObject { ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" };
        var requestParams = new ReadResourceRequestParams
        {
            Uri = "file:///test",
            Meta = meta
        };

        var result = TraceContextPropagator.Extract(requestParams);

        Assert.NotNull(result);
        Assert.Equal("0af7651916cd43dd8448eb211c80319c", result.Value.TraceId.ToString());
        Assert.Equal("b7ad6b7169203331", result.Value.SpanId.ToString());
    }
}
