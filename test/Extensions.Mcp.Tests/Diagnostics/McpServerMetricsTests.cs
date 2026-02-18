// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.Metrics;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests.Diagnostics;

public class McpServerMetricsTests : IDisposable
{
    private readonly MeterListener _listener;
    private readonly List<(string Name, double Value, IReadOnlyList<KeyValuePair<string, object?>> Tags)> _recordedMeasurements = new();

    public McpServerMetricsTests()
    {
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == SemanticConventions.ActivitySourceName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, state) =>
        {
            _recordedMeasurements.Add((instrument.Name, value, tags.ToArray()));
        });

        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    [Fact]
    public void RecordToolDuration_RecordsMeasurement()
    {
        var duration = TimeSpan.FromMilliseconds(150);

        McpServerMetrics.RecordToolDuration(duration, "test-tool", "session-123");

        Assert.Single(_recordedMeasurements);
        var measurement = _recordedMeasurements[0];
        Assert.Equal("mcp.server.operation.duration", measurement.Name);
        Assert.Equal(0.15, measurement.Value, precision: 3);
    }

    [Fact]
    public void RecordToolDuration_SetsMethodNameTag()
    {
        McpServerMetrics.RecordToolDuration(TimeSpan.FromSeconds(1), "test-tool", null);

        var measurement = _recordedMeasurements[0];
        var methodTag = measurement.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Mcp.MethodName);
        Assert.Equal(SemanticConventions.Methods.ToolsCall, methodTag.Value);
    }

    [Fact]
    public void RecordToolDuration_SetsToolNameTag()
    {
        McpServerMetrics.RecordToolDuration(TimeSpan.FromSeconds(1), "my-awesome-tool", null);

        var measurement = _recordedMeasurements[0];
        var toolTag = measurement.Tags.FirstOrDefault(t => t.Key == SemanticConventions.GenAi.ToolName);
        Assert.Equal("my-awesome-tool", toolTag.Value);
    }

    [Fact]
    public void RecordToolDuration_SetsOperationNameTag()
    {
        McpServerMetrics.RecordToolDuration(TimeSpan.FromSeconds(1), "test-tool", null);

        var measurement = _recordedMeasurements[0];
        var operationTag = measurement.Tags.FirstOrDefault(t => t.Key == SemanticConventions.GenAi.OperationName);
        Assert.Equal(SemanticConventions.Operations.ExecuteTool, operationTag.Value);
    }

    [Fact]
    public void RecordToolDuration_SetsSessionIdTag_WhenProvided()
    {
        McpServerMetrics.RecordToolDuration(TimeSpan.FromSeconds(1), "test-tool", "session-xyz");

        var measurement = _recordedMeasurements[0];
        var sessionTag = measurement.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Mcp.SessionId);
        Assert.Equal("session-xyz", sessionTag.Value);
    }

    [Fact]
    public void RecordToolDuration_DoesNotSetSessionIdTag_WhenNull()
    {
        McpServerMetrics.RecordToolDuration(TimeSpan.FromSeconds(1), "test-tool", null);

        var measurement = _recordedMeasurements[0];
        var sessionTag = measurement.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Mcp.SessionId);
        Assert.Equal(default, sessionTag);
    }

    [Fact]
    public void RecordToolDuration_DoesNotSetSessionIdTag_WhenEmpty()
    {
        McpServerMetrics.RecordToolDuration(TimeSpan.FromSeconds(1), "test-tool", "");

        var measurement = _recordedMeasurements[0];
        var sessionTag = measurement.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Mcp.SessionId);
        Assert.Equal(default, sessionTag);
    }

    [Fact]
    public void RecordToolDuration_SetsErrorTypeTag_WhenProvided()
    {
        McpServerMetrics.RecordToolDuration(TimeSpan.FromSeconds(1), "test-tool", null, "tool_error");

        var measurement = _recordedMeasurements[0];
        var errorTag = measurement.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Error.Type);
        Assert.Equal("tool_error", errorTag.Value);
    }

    [Fact]
    public void RecordToolDuration_DoesNotSetErrorTypeTag_WhenNull()
    {
        McpServerMetrics.RecordToolDuration(TimeSpan.FromSeconds(1), "test-tool", null, null);

        var measurement = _recordedMeasurements[0];
        var errorTag = measurement.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Error.Type);
        Assert.Equal(default, errorTag);
    }

    [Fact]
    public void RecordResourceDuration_RecordsMeasurement()
    {
        var duration = TimeSpan.FromMilliseconds(250);

        McpServerMetrics.RecordResourceDuration(duration, "file:///path/to/resource", "session-123");

        Assert.Single(_recordedMeasurements);
        var measurement = _recordedMeasurements[0];
        Assert.Equal("mcp.server.operation.duration", measurement.Name);
        Assert.Equal(0.25, measurement.Value, precision: 3);
    }

    [Fact]
    public void RecordResourceDuration_SetsMethodNameTag()
    {
        McpServerMetrics.RecordResourceDuration(TimeSpan.FromSeconds(1), "file:///path", null);

        var measurement = _recordedMeasurements[0];
        var methodTag = measurement.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Mcp.MethodName);
        Assert.Equal(SemanticConventions.Methods.ResourcesRead, methodTag.Value);
    }

    [Fact]
    public void RecordResourceDuration_SetsResourceUriTag()
    {
        McpServerMetrics.RecordResourceDuration(TimeSpan.FromSeconds(1), "https://example.com/resource", null);

        var measurement = _recordedMeasurements[0];
        var uriTag = measurement.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Mcp.ResourceUri);
        Assert.Equal("https://example.com/resource", uriTag.Value);
    }

    [Fact]
    public void RecordResourceDuration_SetsSessionIdTag_WhenProvided()
    {
        McpServerMetrics.RecordResourceDuration(TimeSpan.FromSeconds(1), "file:///path", "resource-session-456");

        var measurement = _recordedMeasurements[0];
        var sessionTag = measurement.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Mcp.SessionId);
        Assert.Equal("resource-session-456", sessionTag.Value);
    }

    [Fact]
    public void RecordResourceDuration_DoesNotSetSessionIdTag_WhenNull()
    {
        McpServerMetrics.RecordResourceDuration(TimeSpan.FromSeconds(1), "file:///path", null);

        var measurement = _recordedMeasurements[0];
        var sessionTag = measurement.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Mcp.SessionId);
        Assert.Equal(default, sessionTag);
    }

    [Fact]
    public void RecordResourceDuration_SetsErrorTypeTag_WhenProvided()
    {
        McpServerMetrics.RecordResourceDuration(TimeSpan.FromSeconds(1), "file:///path", null, "-32602");

        var measurement = _recordedMeasurements[0];
        var errorTag = measurement.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Error.Type);
        Assert.Equal("-32602", errorTag.Value);
    }

    [Fact]
    public void RecordResourceDuration_DoesNotSetErrorTypeTag_WhenNull()
    {
        McpServerMetrics.RecordResourceDuration(TimeSpan.FromSeconds(1), "file:///path", null, null);

        var measurement = _recordedMeasurements[0];
        var errorTag = measurement.Tags.FirstOrDefault(t => t.Key == SemanticConventions.Error.Type);
        Assert.Equal(default, errorTag);
    }

    [Fact]
    public void RecordToolDuration_ConvertsToSeconds()
    {
        // 500 milliseconds should be 0.5 seconds
        McpServerMetrics.RecordToolDuration(TimeSpan.FromMilliseconds(500), "test-tool", null);

        var measurement = _recordedMeasurements[0];
        Assert.Equal(0.5, measurement.Value, precision: 3);
    }

    [Fact]
    public void RecordResourceDuration_ConvertsToSeconds()
    {
        // 2500 milliseconds should be 2.5 seconds
        McpServerMetrics.RecordResourceDuration(TimeSpan.FromMilliseconds(2500), "file:///path", null);

        var measurement = _recordedMeasurements[0];
        Assert.Equal(2.5, measurement.Value, precision: 3);
    }
}
