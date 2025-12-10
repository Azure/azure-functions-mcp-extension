// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

[AttributeUsage(AttributeTargets.Parameter)]
#pragma warning disable CS0618 // Type or member is obsolete
[Binding(TriggerHandlesReturnValue = true)]
#pragma warning restore CS0618 // Type or member is obsolete
public sealed class McpToolTriggerAttribute(string toolName, string? description) : Attribute
{
    public string ToolName { get; } = toolName;

    public string? Description { get; set; } = description;

    public string? ToolProperties { get; set; }

    public bool UseResultSchema { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the input schema should be provided by the worker instead of being generated from reflection.
    /// When set to true, the <see cref="InputSchema"/> property must be provided.
    /// When set to false (default), the extension generates the schema from the function parameters or <see cref="ToolProperties"/>.
    /// </summary>
    public bool UseWorkerInputSchema { get; set; } = false;

    /// <summary>
    /// Gets or sets the MCP tool input schema as a JSON element.
    /// This should only be set when <see cref="UseWorkerInputSchema"/> is true.
    /// The schema must be a valid JSON Schema object with type "object".
    /// When null and UseWorkerInputSchema is false, the schema is generated from function parameters.
    /// </summary>
    public JsonElement? InputSchema { get; set; }
}
