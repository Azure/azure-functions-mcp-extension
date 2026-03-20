// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ToolTests;

/// <summary>
/// Validates that the worker-side output schema generation produces correct JSON schemas
/// visible through the MCP protocol when listing tools.
/// </summary>
public class OutputSchemaTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    // ToolFunctions currently uses Snippet as an output model for getsnippets.
    // SaveSnippet accepts Snippet as input only, so it is not expected to have an output schema.
    private static readonly string[] SnippetOutputTools = ["getsnippets"];

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task SnippetOutputTools_HaveOutputSchema(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        foreach (var toolName in SnippetOutputTools)
        {
            var tool = tools.FirstOrDefault(t => t.Name == toolName);
            Assert.NotNull(tool);
            Assert.NotNull(tool.ProtocolTool.OutputSchema);
        }
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task GetSnippet_OutputSchema_HasCorrectPropertiesAndTypes(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "getsnippets");
        Assert.NotNull(tool);

        // Snippet class has [McpOutput], so output schema should be auto-generated
        var outputSchema = tool.ProtocolTool.OutputSchema;
        Assert.NotNull(outputSchema);

        var schema = outputSchema.Value;
        Assert.Equal("object", schema.GetProperty("type").GetString());

        var properties = schema.GetProperty("properties");

        // Snippet POCO has Name (string, required) and Content (string?, optional)
        Assert.True(properties.TryGetProperty("name", out var nameProp));
        Assert.Equal("string", nameProp.GetProperty("type").GetString());
        Assert.True(nameProp.TryGetProperty("description", out var nameDesc));
        Assert.Equal("The name of the snippet", nameDesc.GetString());

        Assert.True(properties.TryGetProperty("content", out var contentProp));
        Assert.True(contentProp.TryGetProperty("description", out var contentDesc));
        Assert.Equal("The content of the snippet", contentDesc.GetString());
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task GetSnippet_OutputSchema_HasRequiredProperties(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "getsnippets");
        Assert.NotNull(tool);

        var outputSchema = tool.ProtocolTool.OutputSchema;
        Assert.NotNull(outputSchema);

        var schema = outputSchema.Value;

        // Name is 'required' since it's a non-nullable property
        Assert.True(schema.TryGetProperty("required", out var required));
        var requiredNames = required.EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("name", requiredNames);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ToolsWithoutMcpOutput_HaveNoOutputSchema(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // These tools return string/void/other types without [McpOutput], so should have no output schema
        var toolsWithoutOutputSchema = new[] { "HappyFunction", "SingleArgumentFunction", "BirthdayTracker", "GetFunctionsLogo" };

        foreach (var toolName in toolsWithoutOutputSchema)
        {
            var tool = tools.FirstOrDefault(t => t.Name == toolName);
            Assert.NotNull(tool);

            Assert.Null(tool.ProtocolTool.OutputSchema);
        }
    }
}
