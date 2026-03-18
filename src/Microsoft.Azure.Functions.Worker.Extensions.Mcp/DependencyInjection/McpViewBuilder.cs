// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Azure.Functions.Worker.Builder;

internal sealed class McpViewBuilder : IMcpViewBuilder
{
    private readonly ViewOptions _viewOptions;
    private readonly IMcpAppBuilder _appBuilder;
    private readonly McpToolBuilder _toolBuilder;

    internal McpViewBuilder(ViewOptions viewOptions, IMcpAppBuilder appBuilder, McpToolBuilder toolBuilder)
    {
        _viewOptions = viewOptions;
        _appBuilder = appBuilder;
        _toolBuilder = toolBuilder;
    }

    public IMcpViewBuilder WithTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        _viewOptions.Title = title;
        return this;
    }

    public IMcpViewBuilder WithBorder(bool border = true)
    {
        _viewOptions.Border = border;
        return this;
    }

    public IMcpViewBuilder WithDomain(string domain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);
        _viewOptions.Domain = domain;
        return this;
    }

    public IMcpViewBuilder WithCsp(Action<IMcpCspBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var cspOptions = _viewOptions.Csp ??= new CspOptions();
        var cspBuilder = new McpCspBuilder(cspOptions);
        configure(cspBuilder);
        return this;
    }

    public IMcpViewBuilder WithPermissions(McpAppPermissions permissions)
    {
        _viewOptions.Permissions = permissions;
        return this;
    }

    public IMcpAppBuilder ConfigureApp() => _appBuilder;

    public McpToolBuilder ConfigureTool() => _toolBuilder;
}
