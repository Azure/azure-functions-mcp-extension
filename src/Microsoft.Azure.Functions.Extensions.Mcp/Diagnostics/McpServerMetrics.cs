// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

/// <summary>
/// Metrics for MCP server operations following OTel semantic conventions.
/// </summary>
/// <remarks>
/// Based on: https://opentelemetry.io/docs/specs/semconv/gen-ai/mcp/
///
/// Histogram bucket boundaries (in seconds):
/// [0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 30, 60, 120, 300]
/// </remarks>
internal sealed class McpServerMetrics
{
    private static readonly Meter Meter = new(
        SemanticConventions.ActivitySourceName,
        SemanticConventions.ActivitySourceVersion);

    /// <summary>
    /// Histogram measuring MCP server operation duration in seconds.
    /// </summary>
    private static readonly Histogram<double> OperationDuration = Meter.CreateHistogram<double>(
        name: "mcp.server.operation.duration",
        unit: "s",
        description: "Duration of MCP server operations");

    /// <summary>
    /// Records the duration of a tool execution operation.
    /// </summary>
    /// <param name="duration">The duration of the operation.</param>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="sessionId">The MCP session ID, if available.</param>
    /// <param name="errorType">The error type if the operation failed, null otherwise.</param>
    public static void RecordToolDuration(
        TimeSpan duration,
        string toolName,
        string? sessionId,
        string? errorType = null)
    {
        var tags = new TagList
        {
            { SemanticConventions.Mcp.MethodName, SemanticConventions.Methods.ToolsCall },
            { SemanticConventions.GenAi.ToolName, toolName },
            { SemanticConventions.GenAi.OperationName, SemanticConventions.Operations.ExecuteTool }
        };

        if (!string.IsNullOrEmpty(sessionId))
        {
            tags.Add(SemanticConventions.Mcp.SessionId, sessionId);
        }

        if (!string.IsNullOrEmpty(errorType))
        {
            tags.Add(SemanticConventions.Error.Type, errorType);
        }

        OperationDuration.Record(duration.TotalSeconds, tags);
    }

    /// <summary>
    /// Records the duration of a resource read operation.
    /// </summary>
    /// <param name="duration">The duration of the operation.</param>
    /// <param name="resourceUri">The URI of the resource.</param>
    /// <param name="sessionId">The MCP session ID, if available.</param>
    /// <param name="errorType">The error type if the operation failed, null otherwise.</param>
    public static void RecordResourceDuration(
        TimeSpan duration,
        string resourceUri,
        string? sessionId,
        string? errorType = null)
    {
        var tags = new TagList
        {
            { SemanticConventions.Mcp.MethodName, SemanticConventions.Methods.ResourcesRead },
            { SemanticConventions.Mcp.ResourceUri, resourceUri }
        };

        if (!string.IsNullOrEmpty(sessionId))
        {
            tags.Add(SemanticConventions.Mcp.SessionId, sessionId);
        }

        if (!string.IsNullOrEmpty(errorType))
        {
            tags.Add(SemanticConventions.Error.Type, errorType);
        }

        OperationDuration.Record(duration.TotalSeconds, tags);
    }
}
