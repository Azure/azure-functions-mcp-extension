// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

/// <summary>
/// MCP metrics following OpenTelemetry MCP semantic conventions.
/// See: https://opentelemetry.io/docs/specs/semconv/gen-ai/mcp/
/// </summary>
internal sealed class McpMetrics : IDisposable
{
    private readonly Meter _meter;

    /// <summary>
    /// Histogram bucket boundaries as defined in the MCP semantic conventions.
    /// </summary>
    private static readonly double[] DurationBuckets = [0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 30, 60, 120, 300];

    public McpMetrics()
    {
        _meter = new Meter(TraceConstants.ExtensionActivitySource, TraceConstants.ExtensionActivitySourceVersion);

        ServerOperationDuration = _meter.CreateHistogram<double>(
            name: "mcp.server.operation.duration",
            unit: "s",
            description: "MCP server operation duration");
    }

    /// <summary>
    /// Histogram tracking the duration of MCP server operations.
    /// Dimensions:
    /// - mcp.method.name (required)
    /// - error.type (conditionally required, if error)
    /// - gen_ai.tool.name (conditionally required, for tools/call)
    /// - mcp.resource.uri (conditionally required, for resources/read)
    /// - gen_ai.prompt.name (conditionally required, for prompts/get)
    /// - mcp.session.id (recommended)
    /// - network.transport (recommended)
    /// </summary>
    public Histogram<double> ServerOperationDuration { get; }

    /// <summary>
    /// Records the duration of a tool call operation.
    /// </summary>
    public void RecordToolCallDuration(double durationSeconds, string toolName, string? sessionId = null, string? errorType = null)
    {
        var tags = new TagList
        {
            { TraceConstants.McpAttributes.MethodName, TraceConstants.McpMethods.ToolsCall },
            { TraceConstants.McpAttributes.ToolName, toolName },
            { TraceConstants.McpAttributes.OperationName, TraceConstants.GenAiOperations.ExecuteTool }
        };

        if (!string.IsNullOrEmpty(sessionId))
        {
            tags.Add(TraceConstants.McpAttributes.SessionId, sessionId);
        }

        if (!string.IsNullOrEmpty(errorType))
        {
            tags.Add(TraceConstants.McpAttributes.ErrorType, errorType);
        }

        ServerOperationDuration.Record(durationSeconds, tags);
    }

    /// <summary>
    /// Records the duration of a resource read operation.
    /// </summary>
    public void RecordResourceReadDuration(double durationSeconds, string resourceUri, string? mimeType = null, string? sessionId = null, string? errorType = null)
    {
        var tags = new TagList
        {
            { TraceConstants.McpAttributes.MethodName, TraceConstants.McpMethods.ResourcesRead },
            { TraceConstants.McpAttributes.ResourceUri, resourceUri }
        };

        if (!string.IsNullOrEmpty(mimeType))
        {
            tags.Add("mcp.resource.mime_type", mimeType);
        }

        if (!string.IsNullOrEmpty(sessionId))
        {
            tags.Add(TraceConstants.McpAttributes.SessionId, sessionId);
        }

        if (!string.IsNullOrEmpty(errorType))
        {
            tags.Add(TraceConstants.McpAttributes.ErrorType, errorType);
        }

        ServerOperationDuration.Record(durationSeconds, tags);
    }

    /// <summary>
    /// Records the duration of a prompt get operation.
    /// </summary>
    public void RecordPromptGetDuration(double durationSeconds, string promptName, string? sessionId = null, string? errorType = null)
    {
        var tags = new TagList
        {
            { TraceConstants.McpAttributes.MethodName, TraceConstants.McpMethods.PromptsGet },
            { TraceConstants.McpAttributes.PromptName, promptName }
        };

        if (!string.IsNullOrEmpty(sessionId))
        {
            tags.Add(TraceConstants.McpAttributes.SessionId, sessionId);
        }

        if (!string.IsNullOrEmpty(errorType))
        {
            tags.Add(TraceConstants.McpAttributes.ErrorType, errorType);
        }

        ServerOperationDuration.Record(durationSeconds, tags);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
