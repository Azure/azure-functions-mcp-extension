// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class DefaultResourceRegistryTests
{
    [Fact]
    public void Register_WithValidResource_Succeeds()
    {
        var registry = new DefaultResourceRegistry();
        var resource = CreateTestResource("test://resource/1");

        registry.Register(resource);

        Assert.True(registry.TryGetResource("test://resource/1", out var retrieved));
        Assert.Same(resource, retrieved);
    }

    [Fact]
    public void Register_WithNullResource_ThrowsArgumentNullException()
    {
        var registry = new DefaultResourceRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    [Fact]
    public void Register_WithDuplicateUri_ThrowsInvalidOperationException()
    {
        var registry = new DefaultResourceRegistry();
        var resource1 = CreateTestResource("test://resource/1");
        var resource2 = CreateTestResource("test://resource/1");

        registry.Register(resource1);

        var exception = Assert.Throws<InvalidOperationException>(() => registry.Register(resource2));
        Assert.Contains("already registered", exception.Message);
        Assert.Contains("test://resource/1", exception.Message);
    }

    [Fact]
    public void Register_WithEmptyUri_ThrowsArgumentException()
    {
        var registry = new DefaultResourceRegistry();
        var resource = CreateTestResource("");

        var exception = Assert.Throws<ArgumentException>(() => registry.Register(resource));
        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void Register_WithWhitespaceUri_ThrowsArgumentException()
    {
        var registry = new DefaultResourceRegistry();
        var resource = CreateTestResource("   ");

        var exception = Assert.Throws<ArgumentException>(() => registry.Register(resource));
        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void Register_WithInvalidUriFormat_ThrowsArgumentException()
    {
        var registry = new DefaultResourceRegistry();
        var resource = CreateTestResource("not a valid uri with spaces and : : :");

        var exception = Assert.Throws<ArgumentException>(() => registry.Register(resource));
        Assert.Contains("Invalid resource URI format", exception.Message);
    }

    [Fact]
    public void TryGetResource_WithExistingResource_ReturnsTrue()
    {
        var registry = new DefaultResourceRegistry();
        var resource = CreateTestResource("test://resource/1");
        registry.Register(resource);

        var result = registry.TryGetResource("test://resource/1", out var retrieved);

        Assert.True(result);
        Assert.NotNull(retrieved);
        Assert.Same(resource, retrieved);
    }

    [Fact]
    public void TryGetResource_WithNonExistentResource_ReturnsFalse()
    {
        var registry = new DefaultResourceRegistry();

        var result = registry.TryGetResource("test://resource/nonexistent", out var retrieved);

        Assert.False(result);
        Assert.Null(retrieved);
    }

    [Fact]
    public void TryGetResource_IsCaseInsensitive()
    {
        var registry = new DefaultResourceRegistry();
        var resource = CreateTestResource("test://Resource/1");
        registry.Register(resource);

        var result1 = registry.TryGetResource("test://resource/1", out var retrieved1);
        var result2 = registry.TryGetResource("TEST://RESOURCE/1", out var retrieved2);

        Assert.True(result1);
        Assert.True(result2);
        Assert.Same(resource, retrieved1);
        Assert.Same(resource, retrieved2);
    }

    [Fact]
    public void TryGetResource_WithNullUri_ThrowsArgumentNullException()
    {
        var registry = new DefaultResourceRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.TryGetResource(null!, out _));
    }

    [Fact]
    public void TryGetResource_WithEmptyUri_ThrowsArgumentException()
    {
        var registry = new DefaultResourceRegistry();

        var exception = Assert.Throws<ArgumentException>(() => registry.TryGetResource("", out _));
        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void GetResources_WithNoResources_ReturnsEmptyCollection()
    {
        var registry = new DefaultResourceRegistry();

        var resources = registry.GetResources();

        Assert.NotNull(resources);
        Assert.Empty(resources);
    }

    [Fact]
    public void GetResources_WithMultipleResources_ReturnsAll()
    {
        var registry = new DefaultResourceRegistry();
        var resource1 = CreateTestResource("test://resource/1");
        var resource2 = CreateTestResource("test://resource/2");
        var resource3 = CreateTestResource("test://resource/3");

        registry.Register(resource1);
        registry.Register(resource2);
        registry.Register(resource3);

        var resources = registry.GetResources();

        Assert.Equal(3, resources.Count);
        Assert.Contains(resource1, resources);
        Assert.Contains(resource2, resources);
        Assert.Contains(resource3, resources);
    }

    [Fact]
    public async Task ListResourcesAsync_WithNoResources_ReturnsEmptyList()
    {
        var registry = new DefaultResourceRegistry();

        var result = await registry.ListResourcesAsync();

        Assert.NotNull(result);
        Assert.NotNull(result.Resources);
        Assert.Empty(result.Resources);
    }

    [Fact]
    public async Task ListResourcesAsync_WithResources_ReturnsResourcesList()
    {
        var registry = new DefaultResourceRegistry();
        var resource = CreateTestResource("test://resource/1", name: "TestResource", description: "A test resource");
        registry.Register(resource);

        var result = await registry.ListResourcesAsync();

        Assert.NotNull(result);
        Assert.NotNull(result.Resources);
        Assert.Single(result.Resources);
        var listedResource = result.Resources[0];
        Assert.Equal("test://resource/1", listedResource.Uri);
        Assert.Equal("TestResource", listedResource.Name);
        Assert.Equal("A test resource", listedResource.Description);
    }

    [Fact]
    public async Task ListResourcesAsync_WithMimeType_IncludesMimeType()
    {
        var registry = new DefaultResourceRegistry();
        var resource = CreateTestResource("test://resource/1", mimeType: "text/html");
        registry.Register(resource);

        var result = await registry.ListResourcesAsync();

        var listedResource = result.Resources[0];
        Assert.Equal("text/html", listedResource.MimeType);
    }

    [Fact]
    public async Task ListResourcesAsync_WithSize_IncludesSize()
    {
        var registry = new DefaultResourceRegistry();
        var resource = CreateTestResource("test://resource/1", size: 1024);
        registry.Register(resource);

        var result = await registry.ListResourcesAsync();

        var listedResource = result.Resources[0];
        Assert.Equal(1024, listedResource.Size);
    }

    [Fact]
    public async Task ListResourcesAsync_WithMetadata_IncludesMetaJsonObject()
    {
        var registry = new DefaultResourceRegistry();
        var metadata = new List<IMcpResourceMetadata>
        {
            new TestMetadata("key1", "value1"),
            new TestMetadata("key2", true)
        };
        var resource = CreateTestResource("test://resource/1", metadata: metadata);
        registry.Register(resource);

        var result = await registry.ListResourcesAsync();

        var listedResource = result.Resources[0];
        Assert.NotNull(listedResource.Meta);
        Assert.Equal(2, listedResource.Meta.Count);
        Assert.Equal("value1", listedResource.Meta["key1"]?.GetValue<string>());
        Assert.True(listedResource.Meta["key2"]?.GetValue<bool>());
    }

    [Fact]
    public async Task ListResourcesAsync_WithNullMetadata_ExcludesMeta()
    {
        var registry = new DefaultResourceRegistry();
        var resource = CreateTestResource("test://resource/1", metadata: null);
        registry.Register(resource);

        var result = await registry.ListResourcesAsync();

        var listedResource = result.Resources[0];
        Assert.Null(listedResource.Meta);
    }

    [Fact]
    public async Task ListResourcesAsync_WithEmptyMetadata_ExcludesMeta()
    {
        var registry = new DefaultResourceRegistry();
        var resource = CreateTestResource("test://resource/1", metadata: new List<IMcpResourceMetadata>());
        registry.Register(resource);

        var result = await registry.ListResourcesAsync();

        var listedResource = result.Resources[0];
        Assert.Null(listedResource.Meta);
    }

    [Fact]
    public async Task ListResourcesAsync_WithName_IncludesName()
    {
        var registry = new DefaultResourceRegistry();
        var resource = CreateTestResource("test://resource/1", name: "MyResource");
        registry.Register(resource);

        var result = await registry.ListResourcesAsync();

        var listedResource = result.Resources[0];
        Assert.Equal("MyResource", listedResource.Name);
    }

    [Fact]
    public async Task ListResourcesAsync_WithMultipleResources_ReturnsAllInOrder()
    {
        var registry = new DefaultResourceRegistry();
        var resource1 = CreateTestResource("test://resource/1", name: "First");
        var resource2 = CreateTestResource("test://resource/2", name: "Second");
        var resource3 = CreateTestResource("test://resource/3", name: "Third");

        registry.Register(resource1);
        registry.Register(resource2);
        registry.Register(resource3);

        var result = await registry.ListResourcesAsync();

        Assert.Equal(3, result.Resources.Count);
        Assert.Contains(result.Resources, r => r.Name == "First");
        Assert.Contains(result.Resources, r => r.Name == "Second");
        Assert.Contains(result.Resources, r => r.Name == "Third");
    }

    [Fact]
    public async Task ListResourcesAsync_WithCancellationToken_CompletesSuccessfully()
    {
        var registry = new DefaultResourceRegistry();
        var resource = CreateTestResource("test://resource/1");
        registry.Register(resource);
        using var cts = new CancellationTokenSource();

        var result = await registry.ListResourcesAsync(cts.Token);

        Assert.NotNull(result);
        Assert.Single(result.Resources);
    }

    private static TestResource CreateTestResource(
        string uri,
        string? name = null,
        string? mimeType = null,
        string? description = null,
        long? size = null,
        IReadOnlyCollection<IMcpResourceMetadata>? metadata = null)
    {
        return new TestResource
        {
            Uri = uri,
            Name = name ?? "TestResource",
            MimeType = mimeType,
            Description = description,
            Size = size,
            Metadata = metadata
        };
    }

    private class TestResource : IMcpResource
    {
        public required string Uri { get; init; }
        public required string Name { get; set; }
        public string? MimeType { get; set; }
        public string? Description { get; set; }
        public long? Size { get; set; }
        public IReadOnlyCollection<IMcpResourceMetadata>? Metadata { get; set; }

        public Task<ReadResourceResult> RunAsync(RequestContext<ReadResourceRequestParams> readResourceRequest, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private class TestMetadata : IMcpResourceMetadata
    {
        public TestMetadata(string key, object? value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public object? Value { get; }
    }
}
