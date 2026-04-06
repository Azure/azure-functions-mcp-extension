// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.McpApp;

namespace Worker.Extensions.Mcp.Tests;

public class AppMetadataSerializationTests
{
    [Fact]
    public void BuildToolUiMetadata_ContainsResourceUri()
    {
        var appOptions = CreateMinimalAppOptions();

        var result = AddAppUiMetadataExtension.BuildToolUiMetadata("myTool", appOptions);

        Assert.Equal("ui://myTool/view", result["resourceUri"]!.GetValue<string>());
    }

    [Fact]
    public void BuildToolUiMetadata_ContainsVisibility()
    {
        var appOptions = CreateMinimalAppOptions();
        appOptions.Visibility = McpVisibility.Model | McpVisibility.App;

        var result = AddAppUiMetadataExtension.BuildToolUiMetadata("myTool", appOptions);

        var visibility = result["visibility"]!.AsArray();
        Assert.Equal(2, visibility.Count);
        Assert.Contains("model", visibility.Select(v => v!.GetValue<string>()));
        Assert.Contains("app", visibility.Select(v => v!.GetValue<string>()));
    }

    [Fact]
    public void BuildToolUiMetadata_ModelOnly_Visibility()
    {
        var appOptions = CreateMinimalAppOptions();
        appOptions.Visibility = McpVisibility.Model;

        var result = AddAppUiMetadataExtension.BuildToolUiMetadata("myTool", appOptions);

        var visibility = result["visibility"]!.AsArray();
        Assert.Single(visibility);
        Assert.Equal("model", visibility[0]!.GetValue<string>());
    }

    [Fact]
    public void BuildToolUiMetadata_DoesNotContainCspOrPermissions()
    {
        var appOptions = CreateMinimalAppOptions();

        var result = AddAppUiMetadataExtension.BuildToolUiMetadata("myTool", appOptions);

        // Per spec, CSP/permissions/border go on the resource response, not the tool metadata
        Assert.False(result.ContainsKey("csp"));
        Assert.False(result.ContainsKey("permissions"));
        Assert.False(result.ContainsKey("prefersBorder"));
        Assert.False(result.ContainsKey("border"));
    }

    [Fact]
    public void BuildResourceUiMeta_WithCsp_UsesSpecFieldNames()
    {
        var viewOptions = new ViewOptions
        {
            Source = McpViewSource.FromFile("app.html"),
            Csp = new CspOptions()
        };
        viewOptions.Csp.ConnectDomains.Add("https://api.example.com");
        viewOptions.Csp.ResourceDomains.Add("https://cdn.example.com");
        viewOptions.Csp.FrameDomains.Add("https://youtube.com");
        viewOptions.Csp.BaseUriDomains.Add("https://base.example.com");

        var result = McpAppFunctions.BuildResourceUiMeta(viewOptions);

        Assert.NotNull(result);
        Assert.NotNull(result!.Csp);
        Assert.Contains("https://api.example.com", result.Csp!.ConnectDomains!);
        Assert.Contains("https://cdn.example.com", result.Csp.ResourceDomains!);
        Assert.Contains("https://youtube.com", result.Csp.FrameDomains!);
        Assert.Contains("https://base.example.com", result.Csp.BaseUriDomains!);
    }

    [Fact]
    public void BuildResourceUiMeta_WithPermissions_SetsPermissionObjects()
    {
        var viewOptions = new ViewOptions
        {
            Source = McpViewSource.FromFile("app.html"),
            Permissions = McpAppPermissions.ClipboardRead | McpAppPermissions.ClipboardWrite
        };

        var result = McpAppFunctions.BuildResourceUiMeta(viewOptions);

        Assert.NotNull(result);
        Assert.NotNull(result!.Permissions);
        Assert.NotNull(result.Permissions!.ClipboardRead);
        Assert.NotNull(result.Permissions.ClipboardWrite);
    }

    [Fact]
    public void BuildResourceUiMeta_WithPrefersBorder_True()
    {
        var viewOptions = new ViewOptions
        {
            Source = McpViewSource.FromFile("app.html"),
            PrefersBorder = true
        };

        var result = McpAppFunctions.BuildResourceUiMeta(viewOptions);

        Assert.NotNull(result);
        Assert.True(result!.PrefersBorder);
    }

    [Fact]
    public void BuildResourceUiMeta_WithPrefersBorder_False()
    {
        var viewOptions = new ViewOptions
        {
            Source = McpViewSource.FromFile("app.html"),
            PrefersBorder = false
        };

        var result = McpAppFunctions.BuildResourceUiMeta(viewOptions);

        Assert.NotNull(result);
        Assert.False(result!.PrefersBorder);
    }

    [Fact]
    public void BuildResourceUiMeta_PrefersBorderNull_ReturnsNull()
    {
        var viewOptions = new ViewOptions
        {
            Source = McpViewSource.FromFile("app.html"),
            PrefersBorder = null
        };

        var result = McpAppFunctions.BuildResourceUiMeta(viewOptions);

        Assert.Null(result);
    }

    [Fact]
    public void BuildResourceUiMeta_WithDomain()
    {
        var viewOptions = new ViewOptions
        {
            Source = McpViewSource.FromFile("app.html"),
            Domain = "myapp.example.com"
        };

        var result = McpAppFunctions.BuildResourceUiMeta(viewOptions);

        Assert.NotNull(result);
        Assert.Equal("myapp.example.com", result!.Domain);
    }

    [Fact]
    public void BuildResourceUiMeta_OmitsDefaultValues()
    {
        var viewOptions = new ViewOptions
        {
            Source = McpViewSource.FromFile("app.html")
        };

        var result = McpAppFunctions.BuildResourceUiMeta(viewOptions);

        Assert.Null(result);
    }

    private static AppOptions CreateMinimalAppOptions()
    {
        var options = new AppOptions();
        options.View = new ViewOptions
        {
            Source = McpViewSource.FromFile("app.html")
        };
        return options;
    }
}
