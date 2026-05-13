// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ResourceTests;

/// <summary>
/// Smoke-tests that ListResources and ListResourceTemplates flow end-to-end.
/// Resource metadata content is covered by unit tests against the resource
/// builder and registry: McpResourceBuilderTests, DefaultResourceRegistryTests,
/// ResourceUriHelperTests, AppMetadataSerializationTests.
/// </summary>
public class ListResourceTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    [Theory]
    [MemberData(nameof(TransportModes.All), MemberType = typeof(TransportModes))]
    public async Task ListResources_ReturnsStaticResources(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var resources = await client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(resources);
        Assert.True(resources.Count >= 4, $"Expected at least 4 resources but found {resources.Count}");

        Assert.Contains(resources, r => r.Uri == "file://readme.md");
        Assert.Contains(resources, r => r.Uri == "file://logo.png");
        Assert.Contains(resources, r => r.Uri == "file://minimal.txt");
        Assert.Contains(resources, r => r.Uri == "file://notes.txt");

        // Templates must not leak into the static-resource listing.
        Assert.DoesNotContain(resources, r => r.Uri.Contains('{'));
    }

    [Theory]
    [MemberData(nameof(TransportModes.All), MemberType = typeof(TransportModes))]
    public async Task ListResourceTemplates_ReturnsRegisteredTemplates(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var templates = await client.ListResourceTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(templates);
        Assert.True(templates.Count > 0, "Expected at least one resource template");
        Assert.Contains(templates, t => t.UriTemplate == "user://profile/{name}");
    }
}
