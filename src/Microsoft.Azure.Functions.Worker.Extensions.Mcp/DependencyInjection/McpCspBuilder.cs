// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Azure.Functions.Worker.Builder;

internal sealed class McpCspBuilder : IMcpCspBuilder
{
    private readonly CspOptions _options;

    internal McpCspBuilder(CspOptions options)
    {
        _options = options;
    }

    public IMcpCspBuilder ConnectTo(string origin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        _options.ConnectSources.Add(origin);
        return this;
    }

    public IMcpCspBuilder LoadResourcesFrom(string origin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        _options.ResourceSources.Add(origin);
        return this;
    }

    public IMcpCspBuilder LoadScriptsFrom(string origin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        _options.ScriptSources.Add(origin);
        return this;
    }

    public IMcpCspBuilder LoadStylesFrom(string origin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        _options.StyleSources.Add(origin);
        return this;
    }
}
