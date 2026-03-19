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
        _options.ConnectDomains.Add(origin);
        return this;
    }

    public IMcpCspBuilder LoadResourcesFrom(string origin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        _options.ResourceDomains.Add(origin);
        return this;
    }

    public IMcpCspBuilder AllowFrame(string origin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        _options.FrameDomains.Add(origin);
        return this;
    }

    public IMcpCspBuilder AllowBaseUri(string origin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        _options.BaseUriDomains.Add(origin);
        return this;
    }
}
