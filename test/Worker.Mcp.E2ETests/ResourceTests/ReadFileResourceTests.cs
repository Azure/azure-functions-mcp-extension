// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ResourceTests;

/// <summary>
/// E2E tests for FileResourceContents-based resource functions.
/// Verifies that files returned via FileResourceContents are properly read and
/// converted to TextResourceContents or BlobResourceContents by the middleware.
/// </summary>
public class ReadFileResourceTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    private Uri AppRootEndpoint => Fixture.AppRootEndpoint ?? throw new InvalidOperationException("AppRootEndpoint is not set. Ensure the fixture is initialized properly.");

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListResources_IncludesFileResourceContentsResources(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var resources = await client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(resources);
        Assert.Contains(resources, r => r.Uri == "file://readme-v2.md");
        Assert.Contains(resources, r => r.Uri == "file://logo-v2.png");

        TestOutputHelper.WriteLine($"Resources: {string.Join(", ", resources.Select(r => r.Uri))}");
    }

    [Fact]
    public async Task ReadTextFileResource_ReturnsTextContent()
    {
        var request = ResourceHelper.CreateResourceReadRequest(1, "file://readme-v2.md");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        TestOutputHelper.WriteLine($"ReadResource response: {response}");

        Assert.NotNull(response);
        var jsonString = ResourceHelper.ExtractJsonFromSSE(response);
        var jsonResponse = JsonDocument.Parse(jsonString);
        var root = jsonResponse.RootElement;

        Assert.True(root.TryGetProperty("jsonrpc", out var versionElement));
        Assert.Equal("2.0", versionElement.GetString());

        // Should succeed with text content
        Assert.True(root.TryGetProperty("result", out var resultElement), "Expected successful result");
        Assert.True(resultElement.TryGetProperty("contents", out var contentsArray));

        var contents = contentsArray.EnumerateArray().FirstOrDefault();
        Assert.True(contents.TryGetProperty("text", out var textElement), "Expected text content from FileResourceContents");

        var text = textElement.GetString();
        Assert.NotNull(text);
        Assert.NotEmpty(text);

        TestOutputHelper.WriteLine($"FileResourceContents text content length: {text.Length}");
    }

    [Fact(Skip = "Binary resource results (byte[]) are serialized as base64 strings over gRPC, arriving at the host as text rather than blob content. This is a known platform limitation for isolated worker binary resources.")]
    public async Task ReadBinaryFileResource_ReturnsBlobContent()
    {
        var request = ResourceHelper.CreateResourceReadRequest(2, "file://logo-v2.png");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        TestOutputHelper.WriteLine($"ReadResource response: {response}");

        Assert.NotNull(response);
        var jsonString = ResourceHelper.ExtractJsonFromSSE(response);
        var jsonResponse = JsonDocument.Parse(jsonString);
        var root = jsonResponse.RootElement;

        Assert.True(root.TryGetProperty("jsonrpc", out var versionElement));
        Assert.Equal("2.0", versionElement.GetString());

        // Should succeed with blob content
        Assert.True(root.TryGetProperty("result", out var resultElement), "Expected successful result");
        Assert.True(resultElement.TryGetProperty("contents", out var contentsArray));

        var contents = contentsArray.EnumerateArray().FirstOrDefault();
        Assert.True(contents.TryGetProperty("blob", out var blobElement), "Expected blob content from FileResourceContents with image/png MIME type");

        var blob = blobElement.GetString();
        Assert.NotNull(blob);
        Assert.NotEmpty(blob);

        // Verify it's valid base64
        var bytes = Convert.FromBase64String(blob);
        Assert.True(bytes.Length > 0);

        TestOutputHelper.WriteLine($"FileResourceContents blob content size: {bytes.Length} bytes");
    }

    [Fact]
    public async Task ReadTextFileResource_ContentMatchesOriginalResource()
    {
        // Read the original resource (manual File.ReadAllText)
        var originalRequest = ResourceHelper.CreateResourceReadRequest(1, "file://readme.md");
        var originalResponse = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, originalRequest, TestOutputHelper);

        // Read the FileResourceContents resource (middleware reads the file)
        var fileRequest = ResourceHelper.CreateResourceReadRequest(2, "file://readme-v2.md");
        var fileResponse = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, fileRequest, TestOutputHelper);

        Assert.NotNull(originalResponse);
        Assert.NotNull(fileResponse);

        var originalJson = JsonDocument.Parse(ResourceHelper.ExtractJsonFromSSE(originalResponse));
        var fileJson = JsonDocument.Parse(ResourceHelper.ExtractJsonFromSSE(fileResponse));

        // Both should succeed
        Assert.True(originalJson.RootElement.TryGetProperty("result", out var originalResult), "Original resource should succeed");
        Assert.True(fileJson.RootElement.TryGetProperty("result", out var fileResult), "FileResourceContents resource should succeed");

        // Extract text content from both
        var originalText = originalResult.GetProperty("contents").EnumerateArray().First()
            .GetProperty("text").GetString();
        var fileText = fileResult.GetProperty("contents").EnumerateArray().First()
            .GetProperty("text").GetString();

        // Content should be identical — both serve the same file
        Assert.Equal(originalText, fileText);

        TestOutputHelper.WriteLine("FileResourceContents text content matches original manual resource");
    }

    [Fact(Skip = "Binary resource results (byte[]) are serialized as base64 strings over gRPC, arriving at the host as text rather than blob content. This is a known platform limitation for isolated worker binary resources.")]
    public async Task ReadBinaryFileResource_ContentMatchesOriginalResource()
    {
        // Read the original resource (manual File.ReadAllBytes)
        var originalRequest = ResourceHelper.CreateResourceReadRequest(1, "file://logo.png");
        var originalResponse = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, originalRequest, TestOutputHelper);

        // Read the FileResourceContents resource (middleware reads the file)
        var fileRequest = ResourceHelper.CreateResourceReadRequest(2, "file://logo-v2.png");
        var fileResponse = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, fileRequest, TestOutputHelper);

        Assert.NotNull(originalResponse);
        Assert.NotNull(fileResponse);

        var originalJson = JsonDocument.Parse(ResourceHelper.ExtractJsonFromSSE(originalResponse));
        var fileJson = JsonDocument.Parse(ResourceHelper.ExtractJsonFromSSE(fileResponse));

        // Both should succeed
        Assert.True(originalJson.RootElement.TryGetProperty("result", out var originalResult), "Original resource should succeed");
        Assert.True(fileJson.RootElement.TryGetProperty("result", out var fileResult), "FileResourceContents resource should succeed");

        // Extract blob content from both
        var originalBlob = originalResult.GetProperty("contents").EnumerateArray().First()
            .GetProperty("blob").GetString();
        var fileBlob = fileResult.GetProperty("contents").EnumerateArray().First()
            .GetProperty("blob").GetString();

        // Content should be identical — both serve the same file
        Assert.Equal(originalBlob, fileBlob);

        TestOutputHelper.WriteLine("FileResourceContents blob content matches original manual resource");
    }
}
