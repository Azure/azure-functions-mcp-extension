// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Represents prompt configuration options for metadata and arguments.
/// </summary>
public class PromptOptions : McpBuilderOptions
{
    /// <summary>
    /// Gets the collection of argument definitions for the prompt.
    /// </summary>
    public List<PromptArgumentDefinition> Arguments { get; } = [];

    /// <summary>
    /// Adds a new argument definition to the prompt.
    /// </summary>
    public void AddArgument(string name, string? description = null, bool required = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        Arguments.Add(new PromptArgumentDefinition(name, description, required));
    }

}
