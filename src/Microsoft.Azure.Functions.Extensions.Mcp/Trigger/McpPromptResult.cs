// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Host-side counterpart of the worker SDK's <c>McpPromptResult</c> envelope.
/// Carries a serialized payload (e.g. a <c>GetPromptResult</c> or list of
/// <c>PromptMessage</c>) along with a <see cref="Type"/> discriminator describing
/// how to deserialize <see cref="Content"/>.
/// </summary>
internal sealed class McpPromptResult
{
    /// <summary>
    /// The serialized inner payload.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Discriminator describing the shape of <see cref="Content"/>.
    /// </summary>
    public required string Type { get; init; }
}
