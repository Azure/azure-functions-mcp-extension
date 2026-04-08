// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Globalization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated.Tools;

/// <summary>
/// Tools that test typed parameter binding: scalars, enums, collections, Guid, DateTime.
/// </summary>
public class TypedParameterToolFunctions(ILogger<TypedParameterToolFunctions> logger)
{
    /// <summary>
    /// Tests binding of typed scalar parameters: string, int, bool, and enum.
    /// Example: { "name": "Alice", "job": "FullTime", "age": 30, "isActive": true }
    /// Enum values for job: FullTime, PartTime, Contract, Internship, Temporary, Freelance, Unemployed
    /// </summary>
    [Function(nameof(TypedParametersTool))]
    public string TypedParametersTool(
        [McpToolTrigger(nameof(TypedParametersTool), "Accepts typed scalar parameters and returns a summary.")] ToolInvocationContext context,
        [McpToolProperty("name", "The name of the person.")] string name,
        [McpToolProperty("job", "The job of the person.")] JobType? job,
        [McpToolProperty("age", "The age of the person.")] int age = 20,
        [McpToolProperty("isActive", "Whether the person is active.")] bool isActive = true)
    {
        logger.LogInformation("TypedParametersTool invoked for {Name}", name);
        return $"Name: {name ?? "unknown"} | Job: {job ?? JobType.Unemployed} | Age: {age} | Active: {isActive}";
    }

    /// <summary>
    /// Tests binding of collection/array parameters: string[] and int[].
    /// Example: { "tags": ["azure", "functions", "mcp"], "scores": [95, 87, 100] }
    /// </summary>
    [Function(nameof(CollectionParametersTool))]
    public string CollectionParametersTool(
        [McpToolTrigger(nameof(CollectionParametersTool), "Accepts collection parameters and returns their contents.")] ToolInvocationContext context,
        [McpToolProperty("tags", "A list of string tags.")] IEnumerable<string> tags,
        [McpToolProperty("scores", "A list of integer scores.")] IEnumerable<int> scores)
    {
        logger.LogInformation("CollectionParametersTool invoked");
        var tagsList = tags is ICollection { Count: > 0 } ? string.Join(", ", tags) : "(none)";
        var scoresList = scores is ICollection { Count: > 0 } ? string.Join(", ", scores) : "(none)";
        return $"Tags: [{tagsList}] | Scores: [{scoresList}]";
    }

    /// <summary>
    /// Tests binding of special types: Guid and DateTimeOffset.
    /// Example: { "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890", "timestamp": "2026-04-08T12:00:00Z" }
    /// Note: ISO 8601 date strings are deserialized as DateTimeOffset by the JSON converter.
    /// </summary>
    [Function(nameof(GuidAndDateTimeTool))]
    public string GuidAndDateTimeTool(
        [McpToolTrigger(nameof(GuidAndDateTimeTool), "Accepts a Guid and DateTimeOffset and echoes them back.")] ToolInvocationContext context,
        [McpToolProperty("id", "A unique identifier.")] Guid id,
        [McpToolProperty("timestamp", "A date-time value.")] DateTimeOffset timestamp)
    {
        logger.LogInformation("GuidAndDateTimeTool invoked with id={Id}", id);
        return $"Id: {id} | Timestamp: {timestamp.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)}";
    }

    public enum JobType
    {
        FullTime,
        PartTime,
        Contract,
        Internship,
        Temporary,
        Freelance,
        Unemployed
    }
}
