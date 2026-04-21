// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task PocoOutputTool_HasOutputSchema_WithWeatherResultProperties(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "PocoOutputTool");
        Assert.NotNull(tool);

        var outputSchema = tool.ProtocolTool.OutputSchema;
        Assert.NotNull(outputSchema);

        // WeatherResult has: City (string), Temperature (integer), Condition (string)
        Assert.True(outputSchema.Value.TryGetProperty("properties", out var properties));
        Assert.True(properties.TryGetProperty("City", out var cityProp));
        Assert.Equal("string", cityProp.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("Temperature", out var tempProp));
        Assert.Equal("integer", tempProp.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("Condition", out _));
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task AsyncOutputSchemaTool_HasOutputSchema_WithOrderResultProperties(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "AsyncOutputSchemaTool");
        Assert.NotNull(tool);

        var outputSchema = tool.ProtocolTool.OutputSchema;
        Assert.NotNull(outputSchema);

        // OrderResult has: Item (string), Quantity (integer), Total (number), Status (string)
        Assert.True(outputSchema.Value.TryGetProperty("properties", out var properties));
        Assert.True(properties.TryGetProperty("Item", out var itemProp));
        Assert.Equal("string", itemProp.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("Quantity", out var qtyProp));
        Assert.Equal("integer", qtyProp.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("Total", out _));
        Assert.True(properties.TryGetProperty("Status", out _));
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task FluentOutputSchemaTool_HasOutputSchema_WithExpectedProperties(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "FluentOutputSchemaTool");
        Assert.NotNull(tool);

        var outputSchema = tool.ProtocolTool.OutputSchema;
        Assert.NotNull(outputSchema);

        Assert.True(outputSchema.Value.TryGetProperty("type", out var typeNode));
        Assert.Equal("object", typeNode.GetString());

        Assert.True(outputSchema.Value.TryGetProperty("properties", out var properties));
        Assert.True(properties.TryGetProperty("results", out var resultsProp));
        Assert.Equal("array", resultsProp.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("query", out var queryProp));
        Assert.Equal("string", queryProp.GetProperty("type").GetString());

        Assert.True(outputSchema.Value.TryGetProperty("required", out var required));
        var requiredNames = required.EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("results", requiredNames);
        Assert.Contains("query", requiredNames);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ToolsWithoutMcpOutput_DoNotHaveOutputSchema(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // These tools do NOT have [McpOutput] on their return type and don't use WithOutputSchema
        var toolsWithoutOutputSchema = new[]
        {
            "EchoTool",
            "EchoWithDefault",
            "VoidTool",
            "TextContentTool",
            "TypedParametersTool",
            "FluentDefinedTool",
            "MetadataAttributeTool",
            "PocoInputTool",
        };

        foreach (var toolName in toolsWithoutOutputSchema)
        {
            var tool = tools.FirstOrDefault(t => t.Name == toolName);
            Assert.NotNull(tool);
            Assert.Null(tool.ProtocolTool.OutputSchema);
        }
    }
}
