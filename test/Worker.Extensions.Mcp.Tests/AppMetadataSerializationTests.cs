// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests;

public class AppMetadataSerializationTests
{
    [Fact]
    public void BuildUiMetadata_Visibility_ModelAndApp_SerializesArray()
    {
        var appOptions = CreateMinimalAppOptions();
        appOptions.Visibility = McpVisibility.Model | McpVisibility.App;

        var result = McpFunctionMetadataTransformer.BuildUiMetadata(appOptions);

        var visibility = result["visibility"]!.AsArray();
        Assert.Equal(2, visibility.Count);
        Assert.Contains("model", visibility.Select(v => v!.GetValue<string>()));
        Assert.Contains("app", visibility.Select(v => v!.GetValue<string>()));
    }

    [Fact]
    public void BuildUiMetadata_Visibility_ModelOnly_SerializesArray()
    {
        var appOptions = CreateMinimalAppOptions();
        appOptions.Visibility = McpVisibility.Model;

        var result = McpFunctionMetadataTransformer.BuildUiMetadata(appOptions);

        var visibility = result["visibility"]!.AsArray();
        Assert.Single(visibility);
        Assert.Equal("model", visibility[0]!.GetValue<string>());
    }

    [Fact]
    public void BuildUiMetadata_Visibility_AppOnly_SerializesArray()
    {
        var appOptions = CreateMinimalAppOptions();
        appOptions.Visibility = McpVisibility.App;

        var result = McpFunctionMetadataTransformer.BuildUiMetadata(appOptions);

        var visibility = result["visibility"]!.AsArray();
        Assert.Single(visibility);
        Assert.Equal("app", visibility[0]!.GetValue<string>());
    }

    [Fact]
    public void BuildUiMetadata_Visibility_None_SerializesEmptyArray()
    {
        var appOptions = CreateMinimalAppOptions();
        appOptions.Visibility = McpVisibility.None;

        var result = McpFunctionMetadataTransformer.BuildUiMetadata(appOptions);

        var visibility = result["visibility"]!.AsArray();
        Assert.Empty(visibility);
    }

    [Fact]
    public void SerializePermissions_ClipboardWrite_SerializesKebabCase()
    {
        var result = McpFunctionMetadataTransformer.SerializePermissions(McpAppPermissions.ClipboardWrite);

        Assert.Single(result);
        Assert.Equal("clipboard-write", result[0]!.GetValue<string>());
    }

    [Fact]
    public void SerializePermissions_Both_SerializesKebabCase()
    {
        var result = McpFunctionMetadataTransformer.SerializePermissions(
            McpAppPermissions.ClipboardRead | McpAppPermissions.ClipboardWrite);

        Assert.Equal(2, result.Count);
        Assert.Contains("clipboard-read", result.Select(v => v!.GetValue<string>()));
        Assert.Contains("clipboard-write", result.Select(v => v!.GetValue<string>()));
    }

    [Fact]
    public void BuildCspNode_SerializesDirectives()
    {
        var csp = new CspOptions();
        csp.ConnectSources.Add("https://api.example.com");
        csp.ScriptSources.Add("https://cdn.example.com");
        csp.StyleSources.Add("https://styles.example.com");
        csp.ResourceSources.Add("https://resources.example.com");

        var result = McpFunctionMetadataTransformer.BuildCspNode(csp);

        Assert.Equal("https://api.example.com", result["connect-src"]!.AsArray()[0]!.GetValue<string>());
        Assert.Equal("https://cdn.example.com", result["script-src"]!.AsArray()[0]!.GetValue<string>());
        Assert.Equal("https://styles.example.com", result["style-src"]!.AsArray()[0]!.GetValue<string>());
        Assert.Equal("https://resources.example.com", result["default-src"]!.AsArray()[0]!.GetValue<string>());
    }

    [Fact]
    public void BuildUiMetadata_OmitsDefaultValues()
    {
        var appOptions = CreateMinimalAppOptions();

        var result = McpFunctionMetadataTransformer.BuildUiMetadata(appOptions);

        // Border defaults to false — should not appear
        Assert.False(result.ContainsKey("border"));
        // Permissions default to None — should not appear
        Assert.False(result.ContainsKey("permissions"));
        // CSP not set — should not appear
        Assert.False(result.ContainsKey("csp"));
    }

    [Fact]
    public void BuildUiMetadata_UnnamedView_PropertiesOnUiRoot()
    {
        var appOptions = new AppOptions();
        appOptions.Views[string.Empty] = new ViewOptions
        {
            Source = McpViewSource.FromFile("app.html"),
            Title = "My App",
            Border = true
        };

        var result = McpFunctionMetadataTransformer.BuildUiMetadata(appOptions);

        Assert.Equal("My App", result["title"]!.GetValue<string>());
        Assert.True(result["border"]!.GetValue<bool>());
    }

    [Fact]
    public void BuildUiMetadata_NamedView_NestedUnderViewName()
    {
        var appOptions = new AppOptions();
        appOptions.Views["dashboard"] = new ViewOptions
        {
            Source = McpViewSource.FromFile("dashboard.html"),
            Title = "Dashboard"
        };

        var result = McpFunctionMetadataTransformer.BuildUiMetadata(appOptions);

        var dashboardNode = result["dashboard"]!.AsObject();
        Assert.Equal("Dashboard", dashboardNode["title"]!.GetValue<string>());
    }

    [Fact]
    public void BuildUiMetadata_MultipleViews_EachNested()
    {
        var appOptions = new AppOptions();
        appOptions.Views["main"] = new ViewOptions
        {
            Source = McpViewSource.FromFile("main.html"),
            Title = "Main"
        };
        appOptions.Views["settings"] = new ViewOptions
        {
            Source = McpViewSource.FromFile("settings.html"),
            Title = "Settings"
        };

        var result = McpFunctionMetadataTransformer.BuildUiMetadata(appOptions);

        Assert.Equal("Main", result["main"]!.AsObject()["title"]!.GetValue<string>());
        Assert.Equal("Settings", result["settings"]!.AsObject()["title"]!.GetValue<string>());
    }

    [Fact]
    public void BuildUiMetadata_WithDomain_IncludesDomain()
    {
        var appOptions = new AppOptions();
        appOptions.Views[string.Empty] = new ViewOptions
        {
            Source = McpViewSource.FromFile("app.html"),
            Domain = "myapp.example.com"
        };

        var result = McpFunctionMetadataTransformer.BuildUiMetadata(appOptions);

        Assert.Equal("myapp.example.com", result["domain"]!.GetValue<string>());
    }

    private static AppOptions CreateMinimalAppOptions()
    {
        var options = new AppOptions();
        options.Views[string.Empty] = new ViewOptions
        {
            Source = McpViewSource.FromFile("app.html")
        };
        return options;
    }
}
