// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    public async Task DefaultListResources_ReturnsAllResources(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var resources = await client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(resources);
        Assert.True(resources.Count > 0, "Expected at least one resource to be returned");
        TestOutputHelper.WriteLine($"Found {resources.Count} resources");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListResources_ReturnsExpectedResources(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var resources = await client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(resources);
        // Default server (TestAppIsolated) has these resources registered:
        Assert.Contains(resources, r => r.Uri == "file://readme.md");
        Assert.Contains(resources, r => r.Uri == "file://logo.png");

        TestOutputHelper.WriteLine($"Resources: {string.Join(", ", resources.Select(r => r.Uri))}");;
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListResources_ContainsMetadata(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var resources = await client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(resources);
        var readmeResource = resources.FirstOrDefault(r => r.Uri == "file://readme.md");
        
        Assert.NotNull(readmeResource);
        Assert.Equal("readme", readmeResource.Name);
        Assert.Equal("Application readme file", readmeResource.Description);
        Assert.Equal("text/plain", readmeResource.MimeType);

        // Verify custom McpMetadata attributes are included in _meta
        var meta = readmeResource.ProtocolResource.Meta;
        Assert.NotNull(meta);
        
        // Check flat metadata: [McpMetadata("author", "John Doe")]
        Assert.True(meta.ContainsKey("author"));
        Assert.Equal("John Doe", meta["author"]!.ToString());
        
        // Check nested metadata: [McpMetadata("file:version", "1.0.0")] and [McpMetadata("file:releaseDate", "2024-01-01")]
        Assert.True(meta.ContainsKey("file"));
        var fileNode = meta["file"]!.AsObject();
        Assert.Equal("1.0.0", fileNode["version"]!.ToString());
        Assert.Equal("2024-01-01", fileNode["releaseDate"]!.ToString());

        TestOutputHelper.WriteLine($"Resource: Name={readmeResource.Name}, Description={readmeResource.Description}, MimeType={readmeResource.MimeType}");
        TestOutputHelper.WriteLine($"Metadata: author={meta["author"]}, file.version={fileNode["version"]}, file.releaseDate={fileNode["releaseDate"]}");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListResources_BinaryResourceHasCorrectProperties(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var resources = await client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(resources);
        var logoResource = resources.FirstOrDefault(r => r.Uri == "file://logo.png");
        
        Assert.NotNull(logoResource);
        Assert.Equal("logo", logoResource.Name);
        Assert.Equal("Azure Functions logo", logoResource.Description);
        Assert.Equal("image/png", logoResource.MimeType);

        // This resource has no McpMetadata attributes, so Meta should be null
        Assert.Null(logoResource.ProtocolResource.Meta);

        TestOutputHelper.WriteLine($"Resource: Name={logoResource.Name}, Description={logoResource.Description}, MimeType={logoResource.MimeType}");
    }
}
