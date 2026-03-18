// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal sealed class McpAppBuilder(string toolName) : IMcpAppBuilder
{
    private McpViewBuilder? _viewBuilder;
    private string? _staticAssetsDirectory;
    private McpVisibility _visibility = McpVisibility.ModelAndApp;

    public IMcpAppBuilder WithView(Action<IMcpViewBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _viewBuilder = new McpViewBuilder();
        configure(_viewBuilder);
        return this;
    }

    public IMcpAppBuilder WithStaticAssets(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        _staticAssetsDirectory = directory;
        return this;
    }

    public IMcpAppBuilder WithVisibility(McpVisibility visibility)
    {
        _visibility = visibility;
        return this;
    }

    public AppOptions Build()
    {
        return new AppOptions
        {
            ResourceUri = $"ui://{toolName}/index.html",
            Visibility = _visibility,
            ViewFilePath = _viewBuilder?.FilePath,
            ViewTitle = _viewBuilder?.Title,
            PrefersBorder = _viewBuilder?.PrefersBorder,
            Domain = _viewBuilder?.Domain,
            Csp = _viewBuilder?.Csp,
            Permissions = _viewBuilder?.Permissions ?? McpAppPermissions.None,
            StaticAssetsDirectory = _staticAssetsDirectory
        };
    }
}
