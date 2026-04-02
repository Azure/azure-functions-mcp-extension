// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.McpApp;
using Microsoft.Extensions.Options;

namespace Worker.Extensions.Mcp.Tests;

public class ToolOptionsValidatorTests
{
    private readonly ToolOptionsValidator _validator = new();

    [Fact]
    public void Validate_NullAppOptions_ReturnsSuccess()
    {
        var options = new ToolOptions { Properties = [] };

        var result = _validator.Validate("myTool", options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_EmptyViews_ReturnsFail()
    {
        var options = new ToolOptions
        {
            Properties = [],
            AppOptions = new AppOptions()
        };

        var result = _validator.Validate("myTool", options);

        Assert.True(result.Failed);
        Assert.Contains("no view configured", result.FailureMessage);
    }

    [Fact]
    public void Validate_ViewWithNullSource_ReturnsFail()
    {
        var options = new ToolOptions
        {
            Properties = [],
            AppOptions = new AppOptions()
        };
        options.AppOptions.View = new ViewOptions { Source = null };

        var result = _validator.Validate("myTool", options);

        Assert.True(result.Failed);
        Assert.Contains("no source configured", result.FailureMessage);
    }

    [Fact]
    public void Validate_ViewWithFileSource_ReturnsSuccess()
    {
        var options = new ToolOptions
        {
            Properties = [],
            AppOptions = new AppOptions()
        };
        options.AppOptions.View = new ViewOptions
        {
            Source = McpViewSource.FromFile("ui/app.html")
        };

        var result = _validator.Validate("myTool", options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ViewWithEmbeddedSource_ReturnsSuccess()
    {
        var options = new ToolOptions
        {
            Properties = [],
            AppOptions = new AppOptions()
        };
        options.AppOptions.View = new ViewOptions
        {
            Source = McpViewSource.FromEmbeddedResource("res.html", typeof(ToolOptionsValidatorTests).Assembly)
        };

        var result = _validator.Validate("myTool", options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_EmptyStaticAssetsDirectory_ReturnsFail()
    {
        var options = new ToolOptions
        {
            Properties = [],
            AppOptions = new AppOptions()
        };
        options.AppOptions.View = new ViewOptions
        {
            Source = McpViewSource.FromFile("ok.html")
        };
        options.AppOptions.StaticAssetsDirectory = "   ";

        var result = _validator.Validate("myTool", options);

        Assert.True(result.Failed);
        Assert.Contains("empty StaticAssetsDirectory", result.FailureMessage);
    }

    [Fact]
    public void Validate_ErrorMessage_IncludesToolName()
    {
        var options = new ToolOptions
        {
            Properties = [],
            AppOptions = new AppOptions()
        };

        var result = _validator.Validate("mySpecialTool", options);

        Assert.True(result.Failed);
        Assert.Contains("mySpecialTool", result.FailureMessage);
    }
}
