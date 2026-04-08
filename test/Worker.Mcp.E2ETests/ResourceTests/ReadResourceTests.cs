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
    private Uri AppRootEndpoint => Fixture.AppRootEndpoint ?? throw new InvalidOperationException("AppRootEndpoint is not set.");

    [Fact]
    public async Task ReadTextResource_ReturnsValidJsonRpc()
    {
        var request = ResourceHelper.CreateResourceReadRequest(1, "file://readme.md");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        var jsonString = ResourceHelper.ExtractJsonFromSSE(response);
        var jsonResponse = JsonDocument.Parse(jsonString);
        var root = jsonResponse.RootElement;

        Assert.True(root.TryGetProperty("jsonrpc", out var versionElement));
        Assert.Equal("2.0", versionElement.GetString());

        var hasResult = root.TryGetProperty("result", out _);
        var hasError = root.TryGetProperty("error", out _);
        Assert.True(hasResult || hasError, "Response should contain either result or error");
    }

    [Fact]
    public async Task ReadBinaryResource_ReturnsValidJsonRpc()
    {
        var request = ResourceHelper.CreateResourceReadRequest(2, "file://logo.png");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        var jsonString = ResourceHelper.ExtractJsonFromSSE(response);
        var jsonResponse = JsonDocument.Parse(jsonString);
        var root = jsonResponse.RootElement;

        Assert.True(root.TryGetProperty("jsonrpc", out var versionElement));
        Assert.Equal("2.0", versionElement.GetString());

        var hasResult = root.TryGetProperty("result", out _);
        var hasError = root.TryGetProperty("error", out _);
        Assert.True(hasResult || hasError, "Response should contain either result or error");
    }

    [Fact]
    public async Task ReadMinimalResource_ReturnsContent()
    {
        var request = ResourceHelper.CreateResourceReadRequest(3, "file://minimal.txt");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        var jsonString = ResourceHelper.ExtractJsonFromSSE(response);
        var jsonResponse = JsonDocument.Parse(jsonString);
        var root = jsonResponse.RootElement;

        Assert.True(root.TryGetProperty("jsonrpc", out _));
        Assert.True(root.TryGetProperty("result", out var resultElement), "Expected successful result");
        Assert.True(resultElement.TryGetProperty("contents", out var contentsArray));

        var contents = contentsArray.EnumerateArray().FirstOrDefault();
        Assert.True(contents.TryGetProperty("text", out var textElement), "Expected text content");
        Assert.Contains("Minimal resource content", textElement.GetString());
    }

    [Fact]
    public async Task ReadNotesResource_ReturnsContent()
    {
        var request = ResourceHelper.CreateResourceReadRequest(4, "file://notes.txt");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        var jsonString = ResourceHelper.ExtractJsonFromSSE(response);
        var jsonResponse = JsonDocument.Parse(jsonString);
        var root = jsonResponse.RootElement;

        Assert.True(root.TryGetProperty("jsonrpc", out _));
        Assert.True(root.TryGetProperty("result", out var resultElement), "Expected successful result");
        Assert.True(resultElement.TryGetProperty("contents", out _));
    }

    [Fact]
    public async Task ReadTextResource_WithMetadata_ContainsMetadata()
    {
        var request = ResourceHelper.CreateResourceReadRequest(5, "file://readme.md");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        var jsonString = ResourceHelper.ExtractJsonFromSSE(response);
        var jsonResponse = JsonDocument.Parse(jsonString);
        var root = jsonResponse.RootElement;

        Assert.True(root.TryGetProperty("jsonrpc", out _));

        if (root.TryGetProperty("result", out var resultElement))
        {
            Assert.True(resultElement.TryGetProperty("contents", out var contentsArray));

            var contents = contentsArray.EnumerateArray().FirstOrDefault();

            if (contents.TryGetProperty("metadata", out var metadataElement))
            {
                var metadata = metadataElement.EnumerateObject().ToList();
                var hasAuthor = metadata.Any(prop => prop.Name == "author");
                var hasFile = metadata.Any(prop => prop.Name == "file");

                Assert.True(hasAuthor || hasFile, "Resource should contain metadata attributes");
            }
        }
    }

    [Fact]
    public async Task ReadNonExistentResource_ReturnsError()
    {
        var request = ResourceHelper.CreateResourceReadRequest(6, "file://nonexistent/resource.txt");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
    }
}
