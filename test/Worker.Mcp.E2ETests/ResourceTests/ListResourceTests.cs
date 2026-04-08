// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ResourceTests;

public class ListResourceTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListResources_ReturnsExpectedCount(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var resources = await client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(resources);
        // At minimum, our 4 static resources (readme, logo, minimal, notes) should be present.
        // Templates are excluded from ListResources.
        Assert.True(resources.Count >= 4, $"Expected at least 4 resources but found {resources.Count}");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListResources_ContainsExpectedResources(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var resources = await client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(resources, r => r.Uri == "file://readme.md");
        Assert.Contains(resources, r => r.Uri == "file://logo.png");
        Assert.Contains(resources, r => r.Uri == "file://minimal.txt");
        Assert.Contains(resources, r => r.Uri == "file://notes.txt");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListResources_TextResource_ContainsFullMetadata(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var resources = await client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);

        var readmeResource = resources.FirstOrDefault(r => r.Uri == "file://readme.md");
        Assert.NotNull(readmeResource);
        Assert.Equal("readme", readmeResource.Name);
        Assert.Equal("Application Readme", readmeResource.Title);
        Assert.Equal("Application readme file", readmeResource.Description);
        Assert.Equal("text/plain", readmeResource.MimeType);

        // Verify [McpMetadata] attribute metadata
        var meta = readmeResource.ProtocolResource.Meta;
        Assert.NotNull(meta);

        Assert.True(meta.ContainsKey("author"));
        Assert.Equal("John Doe", meta["author"]!.ToString());

        Assert.True(meta.ContainsKey("file"));
        var fileNode = meta["file"]!.AsObject();
        Assert.Equal(1.0, fileNode["version"]!.GetValue<double>());
        Assert.Equal("2024-01-01", fileNode["releaseDate"]!.ToString());

        Assert.True(meta.ContainsKey("test"));
        var testNode = meta["test"]!.AsObject();
        var exampleArray = testNode["example"]!.AsArray();
        Assert.Equal(3, exampleArray.Count);
        Assert.Equal("list", exampleArray[0]!.ToString());
        Assert.Equal("of", exampleArray[1]!.ToString());
        Assert.Equal("values", exampleArray[2]!.ToString());
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListResources_BinaryResource_HasExpectedProperties(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var resources = await client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);

        var logoResource = resources.FirstOrDefault(r => r.Uri == "file://logo.png");
        Assert.NotNull(logoResource);
        Assert.Equal("logo", logoResource.Name);
        Assert.Equal("Azure Functions logo", logoResource.Description);
        Assert.Equal("image/png", logoResource.MimeType);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListResources_MinimalResource_HasNoOptionalProperties(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var resources = await client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);

        var minimalResource = resources.FirstOrDefault(r => r.Uri == "file://minimal.txt");
        Assert.NotNull(minimalResource);
        Assert.Equal("minimal", minimalResource.Name);

        // Minimal resource has no Title, Description, or MimeType set
        Assert.Null(minimalResource.Title);
        Assert.Null(minimalResource.Description);
        Assert.Null(minimalResource.MimeType);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListResources_NotesResource_ContainsFluentMetadata(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var resources = await client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);

        var notesResource = resources.FirstOrDefault(r => r.Uri == "file://notes.txt");
        Assert.NotNull(notesResource);
        Assert.Equal("notes", notesResource.Name);

        var meta = notesResource.ProtocolResource.Meta;
        Assert.NotNull(meta);
        Assert.True(meta.ContainsKey("category"));
        Assert.Equal("documentation", ((JsonNode)meta["category"]!).GetValue<string>());
        Assert.True(meta.ContainsKey("priority"));
        Assert.Equal(1, ((JsonNode)meta["priority"]!).GetValue<int>());
    }
}
