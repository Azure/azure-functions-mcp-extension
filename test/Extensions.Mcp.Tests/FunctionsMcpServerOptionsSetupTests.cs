// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class FunctionsMcpServerOptionsSetupTests
{
    [Fact]
    public void Configure_SetsServerInfo_FromMcpOptions()
    {
        var mcpOptions = new McpOptions
        {
            ServerName = "Test Server",
            ServerVersion = "1.2.3"
        };
        var optionsWrapper = Options.Create(mcpOptions);
        var setup = new FunctionsMcpServerOptionsSetup(optionsWrapper);
        var serverOptions = new McpServerOptions();

        setup.Configure(serverOptions);

        Assert.NotNull(serverOptions.ServerInfo);
        Assert.Equal("Test Server", serverOptions.ServerInfo.Name);
        Assert.Equal("1.2.3", serverOptions.ServerInfo.Version);
    }

    [Fact]
    public void Configure_SetsServerInstructions_FromMcpOptions()
    {
        var mcpOptions = new McpOptions
        {
            Instructions = "Custom instructions for testing"
        };
        var optionsWrapper = Options.Create(mcpOptions);
        var setup = new FunctionsMcpServerOptionsSetup(optionsWrapper);
        var serverOptions = new McpServerOptions();

        setup.Configure(serverOptions);

        Assert.Equal("Custom instructions for testing", serverOptions.ServerInstructions);
    }

    [Fact]
    public void Configure_SetsCapabilities_WithToolsAndResources()
    {
        var mcpOptions = new McpOptions();
        var optionsWrapper = Options.Create(mcpOptions);
        var setup = new FunctionsMcpServerOptionsSetup(optionsWrapper);
        var serverOptions = new McpServerOptions();

        setup.Configure(serverOptions);

        Assert.NotNull(serverOptions.Capabilities);
        Assert.NotNull(serverOptions.Capabilities.Tools);
        Assert.NotNull(serverOptions.Capabilities.Resources);
    }

    [Fact]
    public void Configure_WithNullOptions_ThrowsArgumentNullException()
    {
        var mcpOptions = new McpOptions();
        var optionsWrapper = Options.Create(mcpOptions);
        var setup = new FunctionsMcpServerOptionsSetup(optionsWrapper);

        Assert.Throws<ArgumentNullException>(() => setup.Configure(null!));
    }

    [Fact]
    public void Configure_WithDefaultMcpOptions_UsesDefaults()
    {
        var mcpOptions = new McpOptions(); // Uses defaults
        var optionsWrapper = Options.Create(mcpOptions);
        var setup = new FunctionsMcpServerOptionsSetup(optionsWrapper);
        var serverOptions = new McpServerOptions();

        setup.Configure(serverOptions);

        Assert.NotNull(serverOptions.ServerInfo);
        Assert.Equal("Azure Functions MCP server", serverOptions.ServerInfo.Name);
        Assert.Equal("1.0.0", serverOptions.ServerInfo.Version);
        Assert.Null(serverOptions.ServerInstructions);
        Assert.NotNull(serverOptions.Capabilities);
        Assert.NotNull(serverOptions.Capabilities.Tools);
        Assert.NotNull(serverOptions.Capabilities.Resources);
    }
}
