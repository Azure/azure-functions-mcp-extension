// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Microsoft.Azure.WebJobs.Hosting;
using System.Text.Json;
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
    public void Format_SerializesCorrectly_UsingSystemTextJson()
    {
        var options = new McpOptions
        {
            ServerName = "Test Server",
            ServerVersion = "2.0.0",
            Instructions = "Sensitive instructions",
            EncryptClientState = false,
            MessageOptions = new MessageOptions
            {
                UseAbsoluteUriForEndpoint = true
            }
        };

        var formatter = (IOptionsFormatter)options;
        var formatted = formatter.Format();

        Assert.NotNull(formatted);
        Assert.Contains("\"ServerName\": \"Test Server\"", formatted);
        Assert.Contains("\"ServerVersion\": \"2.0.0\"", formatted);
        Assert.Contains("\"EncryptClientState\": false", formatted);
        Assert.Contains("\"UseAbsoluteUriForEndpoint\": true", formatted);
        
        // Verify Instructions is NOT included (sensitive data)
        Assert.DoesNotContain("Instructions", formatted);
        Assert.DoesNotContain("Sensitive instructions", formatted);
    }

    [Fact]
    public void Format_ReturnsValidJson()
    {
        var options = new McpOptions();
        var formatter = (IOptionsFormatter)options;
        var formatted = formatter.Format();

        // Should be valid JSON
        var exception = Record.Exception(() => JsonDocument.Parse(formatted));
        Assert.Null(exception);
    }

    [Fact]
    public void Format_IncludesNestedMessageOptions()
    {
        var options = new McpOptions
        {
            MessageOptions = new MessageOptions
            {
                UseAbsoluteUriForEndpoint = true
            }
        };

        var formatter = (IOptionsFormatter)options;
        var formatted = formatter.Format();

        // Should include MessageOptions as a nested object
        Assert.Contains("MessageOptions", formatted);
        Assert.Contains("UseAbsoluteUriForEndpoint", formatted);
    }
}