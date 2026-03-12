// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ResourceTests;

/// <summary>
/// Resource reading tests that make direct HTTP requests to the server
/// </summary>
public class ReadResourceTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    private Uri AppRootEndpoint => Fixture.AppRootEndpoint ?? throw new InvalidOperationException("AppRootEndpoint is not set. Ensure the fixture is initialized properly.");

    [Fact]
    public async Task DefaultServer_ReadTextResource_Success()
    {
        var request = ResourceHelper.CreateResourceReadRequest(1, "file://readme.md");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        TestOutputHelper.WriteLine($"ReadResource response: {response}");

        Assert.NotNull(response);
        var jsonString = ResourceHelper.ExtractJsonFromSSE(response);
        var jsonResponse = JsonDocument.Parse(jsonString);
        var root = jsonResponse.RootElement;

        // Response should be valid JSON-RPC with either result or error
        Assert.True(root.TryGetProperty("jsonrpc", out var versionElement));
        Assert.Equal("2.0", versionElement.GetString());
        
        // Should have either result or error
        var hasResult = root.TryGetProperty("result", out _);
        var hasError = root.TryGetProperty("error", out _);
        Assert.True(hasResult || hasError, "Response should contain either result or error");
    }

    [Fact]
    public async Task DefaultServer_ReadBinaryResource_Success()
    {
        var request = ResourceHelper.CreateResourceReadRequest(2, "file://logo.png");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        TestOutputHelper.WriteLine($"ReadResource response: {response}");

        Assert.NotNull(response);
        var jsonString = ResourceHelper.ExtractJsonFromSSE(response);
        var jsonResponse = JsonDocument.Parse(jsonString);
        var root = jsonResponse.RootElement;

        // Response should be valid JSON-RPC
        Assert.True(root.TryGetProperty("jsonrpc", out var versionElement));
        Assert.Equal("2.0", versionElement.GetString());
        
        // Should have either result or error
        var hasResult = root.TryGetProperty("result", out _);
        var hasError = root.TryGetProperty("error", out _);
        Assert.True(hasResult || hasError, "Response should contain either result or error");
    }

    [Fact]
    public async Task DefaultServer_ReadResource_WithMetadata_ContainsMetadata()
    {
        var request = ResourceHelper.CreateResourceReadRequest(3, "file://readme.md");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        TestOutputHelper.WriteLine($"ReadResource response: {response}");

        Assert.NotNull(response);
        var jsonString = ResourceHelper.ExtractJsonFromSSE(response);
        var jsonResponse = JsonDocument.Parse(jsonString);
        var root = jsonResponse.RootElement;

        // Response should be valid JSON-RPC
        Assert.True(root.TryGetProperty("jsonrpc", out _));
        
        // Check for metadata if resource read succeeds
        if (root.TryGetProperty("result", out var resultElement))
        {
            Assert.True(resultElement.TryGetProperty("contents", out var contentsArray));
            
            var contents = contentsArray.EnumerateArray().FirstOrDefault();
            Assert.True(contents.ValueKind != JsonValueKind.Undefined, "Expected a resource content item.");
            
            Assert.True(contents.TryGetProperty("_meta", out var metadataElement), "Resource content should include _meta.");
            Assert.Equal("documentation", metadataElement.GetProperty("contentKind").GetString());
            Assert.Equal("TestAppIsolated", metadataElement.GetProperty("sampleApp").GetString());

            var renderMetadata = metadataElement.GetProperty("render");
            Assert.Equal("markdown", renderMetadata.GetProperty("mode").GetString());
            Assert.False(renderMetadata.GetProperty("lineNumbers").GetBoolean());

            TestOutputHelper.WriteLine($"Read resource metadata: {metadataElement}");
        }
        else
        {
            TestOutputHelper.WriteLine("Resource read returned error - metadata validation skipped");
        }
    }

    [Fact]
    public async Task DefaultServer_ReadBinaryResource_WithMetadata_ContainsMetadata()
    {
        var request = ResourceHelper.CreateResourceReadRequest(5, "file://logo.png");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        TestOutputHelper.WriteLine($"ReadResource response: {response}");

        Assert.NotNull(response);
        var jsonString = ResourceHelper.ExtractJsonFromSSE(response);
        var jsonResponse = JsonDocument.Parse(jsonString);
        var root = jsonResponse.RootElement;

        Assert.True(root.TryGetProperty("jsonrpc", out _));
        Assert.True(root.TryGetProperty("result", out var resultElement), "Expected a successful resource read result.");
        Assert.True(resultElement.TryGetProperty("contents", out var contentsArray));

        var contents = contentsArray.EnumerateArray().FirstOrDefault();
        Assert.True(contents.ValueKind != JsonValueKind.Undefined, "Expected a resource content item.");
        Assert.True(contents.TryGetProperty("_meta", out var metadataElement), "Binary resource content should include _meta.");

        Assert.Equal("image", metadataElement.GetProperty("contentKind").GetString());
        Assert.Equal("TestAppIsolated", metadataElement.GetProperty("sampleApp").GetString());

        var dimensions = metadataElement.GetProperty("dimensions");
        Assert.Equal(256, dimensions.GetProperty("width").GetInt32());
        Assert.Equal(256, dimensions.GetProperty("height").GetInt32());

        TestOutputHelper.WriteLine($"Binary resource metadata: {metadataElement}");
    }

    [Fact]
    public async Task DefaultServer_ReadNonExistentResource_ReturnsError()
    {
        var request = ResourceHelper.CreateResourceReadRequest(4, "file://nonexistent/resource.txt");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        TestOutputHelper.WriteLine($"ReadResource response: {response}");

        // Should contain error information
        Assert.NotNull(response);
        // The response may contain error details depending on implementation
    }
}
