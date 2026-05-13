// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ToolTests;

/// <summary>
/// Smoke-tests that ListTools flows end-to-end across both transports.
/// Content of the advertised tool metadata (names, schemas, _meta, etc.)
/// is covered by unit tests against the metadata pipeline:
/// McpToolBuilderTests, MetadataParserTests, ResolveToolInputSchemaExtensionTests,
/// ResolveToolOutputSchemaExtensionTests, AddMetadataExtensionTests,
/// AppMetadataSerializationTests, and McpAppFunctionMetadataFactoryTests.
/// </summary>
public class ListToolTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    [Theory]
    [MemberData(nameof(TransportModes.All), MemberType = typeof(TransportModes))]
    public async Task ListTools_ReturnsExpectedTools(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(tools);
        Assert.True(tools.Count >= 20, $"Expected at least 20 tools but found {tools.Count}");

        // A representative tool from each registration style is wired in.
        // Per-tool metadata assertions live in the unit-test suite.
        Assert.Contains(tools, t => t.Name == "EchoTool");           // simple attribute tool
        Assert.Contains(tools, t => t.Name == "PocoInputTool");      // POCO tool
        Assert.Contains(tools, t => t.Name == "FluentDefinedTool");  // fluent builder
        Assert.Contains(tools, t => t.Name == "SchemaTool");         // explicit schema
        Assert.Contains(tools, t => t.Name == "HelloApp");           // MCP app tool
    }
}
