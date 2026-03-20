// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
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
    public async Task HappyFunction_InputSchema_HasCorrectPropertiesAndTypes(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "HappyFunction");
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

        Assert.True(properties.TryGetProperty("attributes", out var attrProp));
        Assert.Equal("array", attrProp.GetProperty("type").GetString());
        Assert.Equal("string", attrProp.GetProperty("items").GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("numbers", out var numProp));
        Assert.Equal("array", numProp.GetProperty("type").GetString());
        Assert.Equal("integer", numProp.GetProperty("items").GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("age", out var ageProp));
        Assert.Equal("integer", ageProp.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("isHappy", out var happyProp));
        Assert.Equal("boolean", happyProp.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task SearchSnippets_InputSchema_HasPocoProperties(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "searchsnippets");
        Assert.NotNull(tool);

        var schema = tool.JsonSchema;
        Assert.Equal("object", schema.GetProperty("type").GetString());

        var properties = schema.GetProperty("properties");

        // SnippetSearchRequest POCO has Pattern (string) and CaseSensitive (bool)
        Assert.True(properties.TryGetProperty("Pattern", out var patternProp));
        Assert.Equal("string", patternProp.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("CaseSensitive", out var caseProp));
        Assert.Equal("boolean", caseProp.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task BirthdayTracker_InputSchema_HasCorrectTypes(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "BirthdayTracker");
        Assert.NotNull(tool);

        var schema = tool.JsonSchema;
        Assert.Equal("object", schema.GetProperty("type").GetString());

        var properties = schema.GetProperty("properties");

        // Guid maps to string type
        Assert.True(properties.TryGetProperty("userId", out var userIdProp));
        Assert.Equal("string", userIdProp.GetProperty("type").GetString());

        // DateTime maps to string type
        Assert.True(properties.TryGetProperty("birthday", out var birthdayProp));
        Assert.Equal("string", birthdayProp.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task SingleArgumentWithDefaultFunction_InputSchema_HasOptionalProperty(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "SingleArgumentWithDefaultFunction");
        Assert.NotNull(tool);

        var schema = tool.JsonSchema;
        Assert.Equal("object", schema.GetProperty("type").GetString());

        var properties = schema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("argument", out var argProp));
        Assert.Equal("string", argProp.GetProperty("type").GetString());

        // argument has a default value, so it should not be required
        var required = schema.GetProperty("required");
        var requiredNames = required.EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.DoesNotContain("argument", requiredNames);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task GetFunctionsLogo_InputSchema_HasNoProperties(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var tool = tools.FirstOrDefault(t => t.Name == "GetFunctionsLogo");
        Assert.NotNull(tool);

        var schema = tool.JsonSchema;
        Assert.Equal("object", schema.GetProperty("type").GetString());

        // Tool with no McpToolProperty parameters should have empty properties
        if (schema.TryGetProperty("properties", out var properties))
        {
            Assert.Empty(properties.EnumerateObject());
        }
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
    public async Task AllTools_InputSchema_PropertyDescriptionsArePopulated(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Verify that tools with properties have descriptions on each property
        var toolsWithProperties = new[] { "HappyFunction", "BirthdayTracker", "SingleArgumentFunction" };

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
