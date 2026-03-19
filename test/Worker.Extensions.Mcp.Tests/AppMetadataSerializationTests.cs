// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests;

public class AppMetadataSerializationTests
{
    [Fact]
    public void BuildToolUiMetadata_ContainsResourceUri()
    {
        var appOptions = CreateMinimalAppOptions();

        var result = McpFunctionMetadataTransformer.BuildToolUiMetadata("myTool", appOptions);

        Assert.Equal("ui://myTool/view", result["resourceUri"]!.GetValue<string>());
    }

    [Fact]
    public void BuildToolUiMetadata_ContainsVisibility()
    {
        var appOptions = CreateMinimalAppOptions();
        appOptions.Visibility = McpVisibility.Model | McpVisibility.App;

        var result = McpFunctionMetadataTransformer.BuildToolUiMetadata("myTool", appOptions);

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

        var result = McpFunctionMetadataTransformer.BuildToolUiMetadata("myTool", appOptions);

        var visibility = result["visibility"]!.AsArray();
        Assert.Single(visibility);
        Assert.Equal("model", visibility[0]!.GetValue<string>());
    }

    [Fact]
    public void BuildToolUiMetadata_DoesNotContainCspOrPermissions()
    {
        var appOptions = CreateMinimalAppOptions();

        var result = McpFunctionMetadataTransformer.BuildToolUiMetadata("myTool", appOptions);

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

        var csp = result["csp"]!.AsObject();
        Assert.Equal("https://api.example.com", csp["connectDomains"]!.AsArray()[0]!.GetValue<string>());
        Assert.Equal("https://cdn.example.com", csp["resourceDomains"]!.AsArray()[0]!.GetValue<string>());
        Assert.Equal("https://youtube.com", csp["frameDomains"]!.AsArray()[0]!.GetValue<string>());
        Assert.Equal("https://base.example.com", csp["baseUriDomains"]!.AsArray()[0]!.GetValue<string>());
    }

    [Fact]
    public void BuildResourceUiMeta_WithPermissions_UsesObjectKeys()
    {
        var viewOptions = new ViewOptions
        {
            Source = McpViewSource.FromFile("app.html"),
            Permissions = McpAppPermissions.ClipboardRead | McpAppPermissions.ClipboardWrite
        };

        var result = McpAppFunctions.BuildResourceUiMeta(viewOptions);

        var perms = result["permissions"]!.AsObject();
        Assert.True(perms.ContainsKey("clipboardRead"));
        Assert.True(perms.ContainsKey("clipboardWrite"));
        // Values are empty objects per spec
        Assert.IsType<JsonObject>(perms["clipboardRead"]);
        Assert.IsType<JsonObject>(perms["clipboardWrite"]);
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

        Assert.True(result["prefersBorder"]!.GetValue<bool>());
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

        Assert.False(result["prefersBorder"]!.GetValue<bool>());
    }

    [Fact]
    public void BuildResourceUiMeta_PrefersBorderNull_Omitted()
    {
        var viewOptions = new ViewOptions
        {
            Source = McpViewSource.FromFile("app.html"),
            PrefersBorder = null
        };

        var result = McpAppFunctions.BuildResourceUiMeta(viewOptions);

        Assert.False(result.ContainsKey("prefersBorder"));
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

        Assert.Equal("myapp.example.com", result["domain"]!.GetValue<string>());
    }

    [Fact]
    public void BuildResourceUiMeta_OmitsDefaultValues()
    {
        var viewOptions = new ViewOptions
        {
            Source = McpViewSource.FromFile("app.html")
        };

        var result = McpAppFunctions.BuildResourceUiMeta(viewOptions);

        Assert.False(result.ContainsKey("csp"));
        Assert.False(result.ContainsKey("permissions"));
        Assert.False(result.ContainsKey("prefersBorder"));
        Assert.False(result.ContainsKey("domain"));
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
