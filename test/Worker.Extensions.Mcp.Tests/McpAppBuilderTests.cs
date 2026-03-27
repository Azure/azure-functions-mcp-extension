// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.McpApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Worker.Extensions.Mcp.Tests;

public class McpAppBuilderTests
{
    private static McpToolBuilder CreateBuilder(string toolName, out ServiceCollection services)
    {
        services = new ServiceCollection();
        services.AddOptions();

        var appBuilder = new Mock<IFunctionsWorkerApplicationBuilder>();
        appBuilder.SetupGet(b => b.Services).Returns(services);

        return new McpToolBuilder(appBuilder.Object, toolName);
    }

    private static ToolOptions GetToolOptions(ServiceCollection services, string toolName)
    {
        using var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);
    }

    [Fact]
    public void WithView_FilePathShorthand_SetsFileViewSource()
    {
        var builder = CreateBuilder("test", out var services);

        builder.AsMcpApp(app => app.WithView("ui/app.html"));

        var options = GetToolOptions(services, "test");
        Assert.NotNull(options.AppOptions);
        Assert.True(options.AppOptions!.View is not null);
        var source = options.AppOptions.View!.Source;
        var fileSource = Assert.IsType<FileViewSource>(source);
        Assert.Equal("ui/app.html", fileSource.Path);
    }

    [Fact]
    public void WithView_McpViewSource_SetsSource()
    {
        var builder = CreateBuilder("test", out var services);

        builder.AsMcpApp(app => app.WithView(McpViewSource.FromFile("ui/app.html")));

        var options = GetToolOptions(services, "test");
        var source = options.AppOptions!.View!.Source;
        var fileSource = Assert.IsType<FileViewSource>(source);
        Assert.Equal("ui/app.html", fileSource.Path);
    }

    [Fact]
    public void WithView_EmbeddedResource_SetsEmbeddedViewSource()
    {
        var builder = CreateBuilder("test", out var services);
        var asm = typeof(McpAppBuilderTests).Assembly;

        builder.AsMcpApp(app => app.WithView(McpViewSource.FromEmbeddedResource("res.html", asm)));

        var options = GetToolOptions(services, "test");
        var source = options.AppOptions!.View!.Source;
        var embeddedSource = Assert.IsType<EmbeddedViewSource>(source);
        Assert.Equal("res.html", embeddedSource.ResourceName);
        Assert.Same(asm, embeddedSource.Assembly);
    }

    [Fact]
    public void WithView_NullFilePath_Throws()
    {
        var builder = CreateBuilder("test", out var services);

        builder.AsMcpApp(app => app.WithView((McpViewSource)null!));

        // Exception occurs when options are resolved (deferred configuration)
        Assert.Throws<ArgumentNullException>(() => GetToolOptions(services, "test"));
    }

    [Fact]
    public void WithView_EmptyFilePath_Throws()
    {
        var builder = CreateBuilder("test", out var services);

        builder.AsMcpApp(app => app.WithView(string.Empty));

        // Exception occurs when options are resolved (deferred configuration)
        Assert.Throws<ArgumentException>(() => GetToolOptions(services, "test"));
    }

    [Fact]
    public void WithTitle_SetsTitle()
    {
        var builder = CreateBuilder("test", out var services);

        builder.AsMcpApp(app => app
            .WithView("ui/app.html")
            .WithTitle("My App"));

        var options = GetToolOptions(services, "test");
        Assert.Equal("My App", options.AppOptions!.View!.Title);
    }

    [Fact]
    public void WithBorder_DefaultTrue_SetsBorderTrue()
    {
        var builder = CreateBuilder("test", out var services);

        builder.AsMcpApp(app => app
            .WithView("ui/app.html")
            .WithBorder());

        var options = GetToolOptions(services, "test");
        Assert.True(options.AppOptions!.View!.PrefersBorder);
    }

    [Fact]
    public void WithBorder_ExplicitFalse_SetsBorderFalse()
    {
        var builder = CreateBuilder("test", out var services);

        builder.AsMcpApp(app => app
            .WithView("ui/app.html")
            .WithBorder(false));

        var options = GetToolOptions(services, "test");
        Assert.False(options.AppOptions!.View!.PrefersBorder);
    }

    [Fact]
    public void WithCsp_AccumulatesAcrossMultipleCalls()
    {
        var builder = CreateBuilder("test", out var services);

        builder.AsMcpApp(app => app
            .WithView("ui/app.html")
            .WithCsp(csp => csp.ConnectTo("a.com"))
            .WithCsp(csp => csp.ConnectTo("b.com")));

        var options = GetToolOptions(services, "test");
        var csp = options.AppOptions!.View!.Csp;
        Assert.NotNull(csp);
        Assert.Contains("a.com", csp!.ConnectDomains);
        Assert.Contains("b.com", csp.ConnectDomains);
    }

    [Fact]
    public void WithVisibility_LastCallWins()
    {
        var builder = CreateBuilder("test", out var services);

        builder.AsMcpApp(app => app
            .WithView("ui/app.html")
            .ConfigureApp()
            .WithVisibility(McpVisibility.Model)
            .WithVisibility(McpVisibility.App));

        var options = GetToolOptions(services, "test");
        Assert.Equal(McpVisibility.App, options.AppOptions!.Visibility);
    }

    [Fact]
    public void WithStaticAssets_SetsDirectory()
    {
        var builder = CreateBuilder("test", out var services);

        builder.AsMcpApp(app => app
            .WithView("ui/app.html")
            .ConfigureApp()
            .WithStaticAssets("ui/dist"));

        var options = GetToolOptions(services, "test");
        Assert.Equal("ui/dist", options.AppOptions!.StaticAssetsDirectory);
    }

    [Fact]
    public void WithStaticAssets_WithOptions_SetsSourceMapFlag()
    {
        var builder = CreateBuilder("test", out var services);

        builder.AsMcpApp(app => app
            .WithView("ui/app.html")
            .ConfigureApp()
            .WithStaticAssets("dir", o => o.IncludeSourceMaps = true));

        var options = GetToolOptions(services, "test");
        Assert.True(options.AppOptions!.StaticAssets?.IncludeSourceMaps);
    }

    [Fact]
    public void ConfigureTool_ReturnsToolBuilder()
    {
        var builder = CreateBuilder("test", out var services);

        var returned = builder.AsMcpApp(app =>
        {
            var toolBuilder = app
                .WithView("ui/app.html")
                .ConfigureTool();

            Assert.Same(builder, toolBuilder);
        });

        Assert.Same(builder, returned);
    }

    [Fact]
    public void WithPermissions_SetsPermissions()
    {
        var builder = CreateBuilder("test", out var services);

        builder.AsMcpApp(app => app
            .WithView("ui/app.html")
            .WithPermissions(McpAppPermissions.ClipboardRead | McpAppPermissions.ClipboardWrite));

        var options = GetToolOptions(services, "test");
        var permissions = options.AppOptions!.View!.Permissions;
        Assert.Equal(McpAppPermissions.ClipboardRead | McpAppPermissions.ClipboardWrite, permissions);
    }

    [Fact]
    public void WithDomain_SetsDomain()
    {
        var builder = CreateBuilder("test", out var services);

        builder.AsMcpApp(app => app
            .WithView("ui/app.html")
            .WithDomain("myapp.example.com"));

        var options = GetToolOptions(services, "test");
        Assert.Equal("myapp.example.com", options.AppOptions!.View!.Domain);
    }

    [Fact]
    public void AsMcpApp_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("test", out _);

        var returned = builder.AsMcpApp(app => app.WithView("ui/app.html"));

        Assert.Same(builder, returned);
    }

    [Fact]
    public void FullChain_CompilesAndConfiguresCorrectly()
    {
        var builder = CreateBuilder("full_tool", out var services);

        builder
            .AsMcpApp(app => app
                .WithView(McpViewSource.FromFile("ui/dist/main.html"))
                .WithTitle("Dashboard")
                .WithBorder()
                .WithDomain("myapp.example.com")
                .WithCsp(csp => csp
                    .ConnectTo("https://api.example.com")
                    .LoadResourcesFrom("https://cdn.example.com"))
                .WithPermissions(McpAppPermissions.ClipboardRead | McpAppPermissions.ClipboardWrite)
                .ConfigureApp()
                .WithStaticAssets("ui/dist", o => o.IncludeSourceMaps = false)
                .WithVisibility(McpVisibility.Model | McpVisibility.App))
            .WithProperty("dataset", McpToolPropertyType.String, "The dataset");

        var options = GetToolOptions(services, "full_tool");
        Assert.NotNull(options.AppOptions);

        var mainView = options.AppOptions!.View!;
        Assert.Equal("Dashboard", mainView.Title);
        Assert.True(mainView.PrefersBorder);
        Assert.Equal("myapp.example.com", mainView.Domain);
        Assert.NotNull(mainView.Csp);
        Assert.Contains("https://api.example.com", mainView.Csp!.ConnectDomains);
        Assert.Contains("https://cdn.example.com", mainView.Csp.ResourceDomains);
        Assert.Equal(McpAppPermissions.ClipboardRead | McpAppPermissions.ClipboardWrite, mainView.Permissions);

        Assert.Equal("ui/dist", options.AppOptions.StaticAssetsDirectory);
        Assert.False(options.AppOptions.StaticAssets?.IncludeSourceMaps);
        Assert.Equal(McpVisibility.Model | McpVisibility.App, options.AppOptions.Visibility);

        Assert.Single(options.Properties);
        Assert.Equal("dataset", options.Properties[0].Name);
    }
}
