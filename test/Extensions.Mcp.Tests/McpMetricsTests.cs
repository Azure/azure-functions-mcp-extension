// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.Metrics;
using Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpMetricsTests : IDisposable
{
    private readonly MeterListener _meterListener;
    private readonly List<(string Name, double Value, KeyValuePair<string, object?>[] Tags)> _recordedMeasurements;

    public McpMetricsTests()
    {
        _recordedMeasurements = [];
        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "Azure.Functions.Extensions.Mcp")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            _recordedMeasurements.Add((instrument.Name, measurement, tags.ToArray()));
        });
        _meterListener.Start();
    }

    public void Dispose()
    {
        _meterListener.Dispose();
    }

    [Fact]
    public void RecordToolCallDuration_RecordsMetricWithCorrectTags()
    {
        var metrics = new McpMetrics();

        metrics.RecordToolCallDuration(
            durationSeconds: 1.5,
            toolName: "myTool",
            sessionId: "session123",
            errorType: null);

        Assert.Single(_recordedMeasurements);
        var (name, value, tags) = _recordedMeasurements[0];

        Assert.Equal("mcp.server.operation.duration", name);
        Assert.Equal(1.5, value);
        Assert.Contains(tags, t => t.Key == "mcp.method.name" && (string?)t.Value == "tools/call");
        Assert.Contains(tags, t => t.Key == "gen_ai.tool.name" && (string?)t.Value == "myTool");
        Assert.Contains(tags, t => t.Key == "mcp.session.id" && (string?)t.Value == "session123");
    }

    [Fact]
    public void RecordToolCallDuration_RecordsErrorType_WhenProvided()
    {
        var metrics = new McpMetrics();

        metrics.RecordToolCallDuration(
            durationSeconds: 0.5,
            toolName: "failingTool",
            sessionId: null,
            errorType: "System.InvalidOperationException");

        Assert.Single(_recordedMeasurements);
        var (_, _, tags) = _recordedMeasurements[0];

        Assert.Contains(tags, t => t.Key == "error.type" && (string?)t.Value == "System.InvalidOperationException");
    }

    [Fact]
    public void RecordResourceReadDuration_RecordsMetricWithCorrectTags()
    {
        var metrics = new McpMetrics();

        metrics.RecordResourceReadDuration(
            durationSeconds: 2.0,
            resourceUri: "file://readme.md",
            mimeType: "text/plain",
            sessionId: "session456",
            errorType: null);

        Assert.Single(_recordedMeasurements);
        var (name, value, tags) = _recordedMeasurements[0];

        Assert.Equal("mcp.server.operation.duration", name);
        Assert.Equal(2.0, value);
        Assert.Contains(tags, t => t.Key == "mcp.method.name" && (string?)t.Value == "resources/read");
        Assert.Contains(tags, t => t.Key == "mcp.resource.uri" && (string?)t.Value == "file://readme.md");
        Assert.Contains(tags, t => t.Key == "mcp.resource.mime_type" && (string?)t.Value == "text/plain");
        Assert.Contains(tags, t => t.Key == "mcp.session.id" && (string?)t.Value == "session456");
    }

    [Fact]
    public void RecordResourceReadDuration_OmitsMimeType_WhenNull()
    {
        var metrics = new McpMetrics();

        metrics.RecordResourceReadDuration(
            durationSeconds: 1.0,
            resourceUri: "file://data.bin",
            mimeType: null,
            sessionId: null,
            errorType: null);

        Assert.Single(_recordedMeasurements);
        var (_, _, tags) = _recordedMeasurements[0];

        Assert.DoesNotContain(tags, t => t.Key == "mcp.resource.mime_type");
    }

    [Fact]
    public void RecordResourceReadDuration_RecordsErrorType_WhenProvided()
    {
        var metrics = new McpMetrics();

        metrics.RecordResourceReadDuration(
            durationSeconds: 0.1,
            resourceUri: "file://missing.txt",
            mimeType: null,
            sessionId: null,
            errorType: "System.IO.FileNotFoundException");

        Assert.Single(_recordedMeasurements);
        var (_, _, tags) = _recordedMeasurements[0];

        Assert.Contains(tags, t => t.Key == "error.type" && (string?)t.Value == "System.IO.FileNotFoundException");
    }

    [Fact]
    public void RecordPromptGetDuration_RecordsMetricWithCorrectTags()
    {
        var metrics = new McpMetrics();

        metrics.RecordPromptGetDuration(
            durationSeconds: 0.75,
            promptName: "greeting",
            sessionId: "session789",
            errorType: null);

        Assert.Single(_recordedMeasurements);
        var (name, value, tags) = _recordedMeasurements[0];

        Assert.Equal("mcp.server.operation.duration", name);
        Assert.Equal(0.75, value);
        Assert.Contains(tags, t => t.Key == "mcp.method.name" && (string?)t.Value == "prompts/get");
        Assert.Contains(tags, t => t.Key == "gen_ai.prompt.name" && (string?)t.Value == "greeting");
        Assert.Contains(tags, t => t.Key == "mcp.session.id" && (string?)t.Value == "session789");
    }
}
