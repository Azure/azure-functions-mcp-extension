// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ToolTests;

/// <summary>
/// Validates that the worker-side input schema generation produces correct JSON schemas
/// visible through the MCP protocol when listing tools.
/// </summary>
public class InputSchemaTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task TypedParametersTool_InputSchema_HasCorrectPropertiesAndTypes(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "TypedParametersTool");
        Assert.NotNull(tool);

        var schema = tool.JsonSchema;
        Assert.Equal("object", schema.GetProperty("type").GetString());

        var properties = schema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("name", out var nameProp));
        Assert.Equal("string", nameProp.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("job", out var jobProp));
        Assert.Equal("string", jobProp.GetProperty("type").GetString());
        Assert.True(jobProp.TryGetProperty("enum", out var enumValues));
        Assert.True(enumValues.GetArrayLength() > 0, "Enum values should be present for JobType");

        Assert.True(properties.TryGetProperty("age", out var ageProp));
        Assert.Equal("integer", ageProp.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("isActive", out var activeProp));
        Assert.Equal("boolean", activeProp.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task CollectionParametersTool_InputSchema_HasArrayTypes(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "CollectionParametersTool");
        Assert.NotNull(tool);

        var schema = tool.JsonSchema;
        Assert.Equal("object", schema.GetProperty("type").GetString());

        var properties = schema.GetProperty("properties");

        Assert.True(properties.TryGetProperty("tags", out var tagsProp));
        Assert.Equal("array", tagsProp.GetProperty("type").GetString());
        Assert.Equal("string", tagsProp.GetProperty("items").GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("scores", out var scoresProp));
        Assert.Equal("array", scoresProp.GetProperty("type").GetString());
        Assert.Equal("integer", scoresProp.GetProperty("items").GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task GuidAndDateTimeTool_InputSchema_HasStringTypes(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "GuidAndDateTimeTool");
        Assert.NotNull(tool);

        var schema = tool.JsonSchema;
        Assert.Equal("object", schema.GetProperty("type").GetString());

        var properties = schema.GetProperty("properties");

        // Guid maps to string type
        Assert.True(properties.TryGetProperty("id", out var idProp));
        Assert.Equal("string", idProp.GetProperty("type").GetString());

        // DateTimeOffset maps to string type
        Assert.True(properties.TryGetProperty("timestamp", out var tsProp));
        Assert.Equal("string", tsProp.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task PocoInputTool_InputSchema_HasPocoProperties(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "PocoInputTool");
        Assert.NotNull(tool);

        var schema = tool.JsonSchema;
        Assert.Equal("object", schema.GetProperty("type").GetString());

        var properties = schema.GetProperty("properties");

        Assert.True(properties.TryGetProperty("Name", out var nameProp));
        Assert.Equal("string", nameProp.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("Age", out var ageProp));
        Assert.Equal("integer", ageProp.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("IsPremium", out var premiumProp));
        Assert.Equal("boolean", premiumProp.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task EchoWithDefault_InputSchema_HasOptionalProperty(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "EchoWithDefault");
        Assert.NotNull(tool);

        var schema = tool.JsonSchema;
        Assert.Equal("object", schema.GetProperty("type").GetString());

        var properties = schema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("message", out var msgProp));
        Assert.Equal("string", msgProp.GetProperty("type").GetString());

        // message has a default value, so it should not be in required
        Assert.True(schema.TryGetProperty("required", out var required));
        var requiredNames = required.EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.DoesNotContain("message", requiredNames);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task VoidTool_InputSchema_HasExpectedProperty(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "VoidTool");
        Assert.NotNull(tool);

        var schema = tool.JsonSchema;
        Assert.Equal("object", schema.GetProperty("type").GetString());

        var properties = schema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("input", out var inputProp));
        Assert.Equal("string", inputProp.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task AllTools_InputSchema_HasObjectType(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotEmpty(tools);

        foreach (var tool in tools)
        {
            var schema = tool.JsonSchema;
            Assert.Equal("object", schema.GetProperty("type").GetString());
        }
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ToolsWithProperties_InputSchema_DescriptionsArePopulated(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Verify that tools with McpToolProperty attributes have descriptions on each property
        var toolsWithProperties = new[] { "TypedParametersTool", "EchoTool", "GuidAndDateTimeTool" };

        foreach (var toolName in toolsWithProperties)
        {
            var tool = tools.FirstOrDefault(t => t.Name == toolName);
            Assert.NotNull(tool);

            var schema = tool.JsonSchema;
            if (schema.TryGetProperty("properties", out var properties))
            {
                foreach (var prop in properties.EnumerateObject())
                {
                    Assert.True(prop.Value.TryGetProperty("description", out var desc),
                        $"Property '{prop.Name}' on tool '{toolName}' should have a description");
                    Assert.False(string.IsNullOrEmpty(desc.GetString()),
                        $"Property '{prop.Name}' on tool '{toolName}' should have a non-empty description");
                }
            }
        }
    }
}
