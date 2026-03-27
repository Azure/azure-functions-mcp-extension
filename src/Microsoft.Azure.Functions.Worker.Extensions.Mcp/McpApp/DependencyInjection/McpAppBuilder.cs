// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.McpApp;

namespace Microsoft.Azure.Functions.Worker.Builder;

internal sealed class McpAppBuilder : IMcpAppBuilder
{
    private readonly AppOptions _appOptions;
    private readonly McpToolBuilder _toolBuilder;

    internal McpAppBuilder(AppOptions appOptions, McpToolBuilder toolBuilder)
    {
        _appOptions = appOptions;
        _toolBuilder = toolBuilder;
    }

    public IMcpViewBuilder WithView(McpViewSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var viewOptions = new ViewOptions { Source = source };
        _appOptions.View = viewOptions;
        return new McpViewBuilder(viewOptions, this, _toolBuilder);
    }

    public IMcpViewBuilder WithView(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var viewOptions = new ViewOptions { Source = McpViewSource.FromFile(filePath) };
        _appOptions.View = viewOptions;
        return new McpViewBuilder(viewOptions, this, _toolBuilder);
    }

    public IMcpAppBuilder WithStaticAssets(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        _appOptions.StaticAssetsDirectory = directory;
        return this;
    }

    public IMcpAppBuilder WithStaticAssets(string directory, Action<StaticAssetOptions> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(configure);

        _appOptions.StaticAssetsDirectory = directory;
        var options = new StaticAssetOptions();
        configure(options);
        _appOptions.StaticAssets = options;
        return this;
    }

    public IMcpAppBuilder WithVisibility(McpVisibility visibility)
    {
        _appOptions.Visibility = visibility;
        return this;
    }
}
