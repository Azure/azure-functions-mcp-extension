// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal sealed class McpCspBuilder : IMcpCspBuilder
{
    private List<string>? _connectDomains;
    private List<string>? _resourceDomains;

    public IMcpCspBuilder ConnectTo(string domain)
    {
        ArgumentException.ThrowIfNullOrEmpty(domain);
        _connectDomains ??= [];
        _connectDomains.Add(domain);
        return this;
    }

    public IMcpCspBuilder LoadResourcesFrom(string domain)
    {
        ArgumentException.ThrowIfNullOrEmpty(domain);
        _resourceDomains ??= [];
        _resourceDomains.Add(domain);
        return this;
    }

    public AppCspOptions Build()
    {
        return new AppCspOptions
        {
            ConnectDomains = _connectDomains,
            ResourceDomains = _resourceDomains
        };
    }
}
