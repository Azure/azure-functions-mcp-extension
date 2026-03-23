// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;

/// <summary>
/// Represents a registry for MCP prompts.
/// </summary>
internal interface IPromptRegistry
{
    /// <summary>
    /// Registers an MCP prompt.
    /// </summary>
    /// <param name="prompt">The prompt to register.</param>
    void Register(IMcpPrompt prompt);

    /// <summary>
    /// Tries to get an MCP prompt by its name.
    /// </summary>
    /// <param name="name">The name of the prompt.</param>
    /// <param name="prompt">The retrieved prompt, if found.</param>
    /// <returns>True if the prompt was found; otherwise, false.</returns>
    bool TryGetPrompt(string name, [NotNullWhen(true)] out IMcpPrompt? prompt);

    /// <summary>
    /// Gets all registered MCP prompts.
    /// </summary>
    IReadOnlyCollection<IMcpPrompt> GetPrompts();

    /// <summary>
    /// Lists all registered MCP prompts.
    /// </summary>
    ValueTask<ListPromptsResult> ListPromptsAsync(CancellationToken cancellationToken = default);
}
