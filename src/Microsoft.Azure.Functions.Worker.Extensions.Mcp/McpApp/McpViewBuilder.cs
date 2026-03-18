// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal sealed class McpViewBuilder : IMcpViewBuilder
{
    private string? _filePath;
    private string? _title;
    private bool? _prefersBorder;
    private string? _domain;
    private AppCspOptions? _csp;
    private McpAppPermissions _permissions = McpAppPermissions.None;

    public IMcpViewBuilder FromFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        _filePath = filePath;
        return this;
    }

    public IMcpViewBuilder WithTitle(string title)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        _title = title;
        return this;
    }

    public IMcpViewBuilder WithBorder()
    {
        _prefersBorder = true;
        return this;
    }

    public IMcpViewBuilder WithoutBorder()
    {
        _prefersBorder = false;
        return this;
    }

    public IMcpViewBuilder WithDomain(string domain)
    {
        ArgumentException.ThrowIfNullOrEmpty(domain);
        _domain = domain;
        return this;
    }

    public IMcpViewBuilder WithCsp(Action<IMcpCspBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var cspBuilder = new McpCspBuilder();
        configure(cspBuilder);
        _csp = cspBuilder.Build();
        return this;
    }

    public IMcpViewBuilder WithPermissions(McpAppPermissions permissions)
    {
        _permissions = permissions;
        return this;
    }

    public string? FilePath => _filePath;
    public string? Title => _title;
    public bool? PrefersBorder => _prefersBorder;
    public string? Domain => _domain;
    public AppCspOptions? Csp => _csp;
    public McpAppPermissions Permissions => _permissions;
}
