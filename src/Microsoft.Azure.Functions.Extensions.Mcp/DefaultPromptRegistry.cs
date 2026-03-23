// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using ModelContextProtocol.Protocol;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Default implementation of <see cref="IPromptRegistry"/>.
/// </summary>
internal sealed class DefaultPromptRegistry : IPromptRegistry
{
    private readonly Dictionary<string, IMcpPrompt> _prompts = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public void Register(IMcpPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        if (!_prompts.TryAdd(prompt.Name, prompt))
        {
            throw new InvalidOperationException($"Prompt with name '{prompt.Name}' is already registered.");
        }
    }

    /// <inheritdoc/>
    public bool TryGetPrompt(string name, [NotNullWhen(true)] out IMcpPrompt? prompt)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _prompts.TryGetValue(name, out prompt);
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<IMcpPrompt> GetPrompts()
    {
        return _prompts.Values;
    }

    /// <inheritdoc/>
    public ValueTask<ListPromptsResult> ListPromptsAsync(CancellationToken cancellationToken = default)
    {
        var result = new ListPromptsResult
        {
            Prompts = _prompts.Values.Select(static prompt => new Prompt
            {
                Name = prompt.Name,
                Title = prompt.Title,
                Description = prompt.Description,
                Arguments = prompt.Arguments?.ToList(),
                Icons = prompt.Icons,
                Meta = MetadataParser.SerializeMetadata(prompt.Metadata)
            }).ToList()
        };

        return new ValueTask<ListPromptsResult>(result);
    }
}
