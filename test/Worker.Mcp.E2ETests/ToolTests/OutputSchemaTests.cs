// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ToolTests;

/// <summary>
/// Validates that an explicit output schema configured via
/// <c>ConfigureMcpTool().WithOutputSchema(...)</c> is advertised through the MCP
/// protocol when listing tools.
/// </summary>
public class OutputSchemaTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
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
    public async Task ToolsWithoutOutputSchema_DoNotHaveOutputSchema(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

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