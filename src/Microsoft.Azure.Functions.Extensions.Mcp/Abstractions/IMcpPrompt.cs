// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Represents a Model Context Protocol (MCP) prompt.
/// </summary>
internal interface IMcpPrompt
{
    /// <summary>
    /// Gets the unique name of the prompt.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets or sets an optional human-readable title for display purposes.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// Gets or sets an optional description of the prompt.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the list of arguments for the prompt.
    /// </summary>
    IReadOnlyList<PromptArgument>? Arguments { get; }

    /// <summary>
    /// Gets optional icons for display in user interfaces.
    /// </summary>
    IList<Icon>? Icons { get; }

    /// <summary>
    /// Gets or sets metadata properties associated with the prompt.
    /// </summary>
    IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Handles a get request for the prompt.
    /// </summary>
    /// <param name="getPromptRequest">The get prompt request context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the prompt result.</returns>
    Task<GetPromptResult> GetAsync(RequestContext<GetPromptRequestParams> getPromptRequest, CancellationToken cancellationToken);
}
