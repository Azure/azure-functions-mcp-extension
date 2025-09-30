// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Microsoft.Azure.WebJobs.Hosting;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpOptionsTests
{
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        var options = new McpOptions();

        Assert.Equal("Azure Functions MCP server", options.ServerName);
        Assert.Equal("1.0.0", options.ServerVersion);
        Assert.Null(options.Instructions);
        Assert.True(options.EncryptClientState);
        Assert.NotNull(options.MessageOptions);
        Assert.False(options.MessageOptions.UseAbsoluteUriForEndpoint);
    }

    [Fact]
    public void Format_ReturnsExpectedJson()
    {
        var options = new McpOptions
        {
            ServerName = "Test Server",
            ServerVersion = "2.0.0",
            Instructions = "Test instructions",
            EncryptClientState = false
        };
        options.MessageOptions.UseAbsoluteUriForEndpoint = true;

        var formatter = (IOptionsFormatter)options;
        var formatted = formatter.Format();

        Assert.NotNull(formatted);
        
        var json = JObject.Parse(formatted);
        Assert.Equal("Test Server", json[nameof(options.ServerName)]?.ToString());
        Assert.Equal("2.0.0", json[nameof(options.ServerVersion)]?.ToString());
        Assert.Equal("Test instructions", json[nameof(options.Instructions)]?.ToString());
        Assert.Equal("False", json[nameof(options.EncryptClientState)]?.ToString());
        Assert.Equal("True", json["MessageOptions.UseAbsoluteUriForEndpoint"]?.ToString());
    }

    [Fact]
    public void Format_IncludesAllNonSensitiveProperties()
    {
        var options = new McpOptions();
        var formatter = (IOptionsFormatter)options;
        var formatted = formatter.Format();

        var json = JObject.Parse(formatted);
        
        Assert.Contains(nameof(options.ServerName), json.Properties().Select(p => p.Name));
        Assert.Contains(nameof(options.ServerVersion), json.Properties().Select(p => p.Name));
        Assert.Contains(nameof(options.Instructions), json.Properties().Select(p => p.Name));
        Assert.Contains(nameof(options.EncryptClientState), json.Properties().Select(p => p.Name));
        Assert.Contains("MessageOptions.UseAbsoluteUriForEndpoint", json.Properties().Select(p => p.Name));
    }
}