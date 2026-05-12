// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ResourceTests;

/// <summary>
/// End-to-end coverage for resource reading. Content shape, MIME handling,
/// and metadata serialization are covered by unit tests:
/// DefaultResourceRegistryTests, ResourceReturnValueBinderTests,
/// McpResourceTriggerBindingTests, ResourceUriHelperTests.
///
/// We keep one text read, one binary read, one template-parameter read, one
/// literal-separated template read, and one error path here.
/// </summary>
public class ReadResourceTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    private Uri AppRootEndpoint => Fixture.AppRootEndpoint ?? throw new InvalidOperationException("AppRootEndpoint is not set.");

    [Fact]
    public async Task ReadTextResource_ReturnsContent()
    {
        var request = ResourceHelper.CreateResourceReadRequest(1, "file://minimal.txt");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        var contents = AssertSuccessfulResourceRead(response);
        Assert.True(contents.TryGetProperty("text", out var textElement), "Expected text content");
        Assert.Contains("Minimal resource content", textElement.GetString());
    }

    [Fact]
    public async Task ReadBinaryResource_ReturnsBlobOrText()
    {
        var request = ResourceHelper.CreateResourceReadRequest(1, "file://logo.png");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        var contents = AssertSuccessfulResourceRead(response);
        Assert.True(
            contents.TryGetProperty("blob", out _) || contents.TryGetProperty("text", out _),
            "Expected blob or text content for the binary resource");
    }

    [Fact]
    public async Task ReadResourceTemplate_BindsParameter()
    {
        var request = ResourceHelper.CreateResourceReadRequest(1, "user://profile/bob");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        var contents = AssertSuccessfulResourceRead(response);
        Assert.True(
            contents.TryGetProperty("text", out _) || contents.TryGetProperty("blob", out _),
            "Expected text or blob content from the template");
    }

    [Fact]
    public async Task ReadResourceTemplate_LiteralSeparator_ExtractsParameters()
    {
        // The URI template "store://catalog/{category}items{tag}" uses a literal
        // separator (not a URI delimiter) between two parameters. The function
        // echoes back the extracted parameters as JSON.
        var request = ResourceHelper.CreateResourceReadRequest(1, "store://catalog/booksitemsfiction");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        var contents = AssertSuccessfulResourceRead(response);
        Assert.True(contents.TryGetProperty("text", out var textElement), "Expected text content");

        var echoed = JsonDocument.Parse(textElement.GetString()!);
        Assert.Equal("books", echoed.RootElement.GetProperty("category").GetString());
        Assert.Equal("fiction", echoed.RootElement.GetProperty("tag").GetString());
    }

    [Fact]
    public async Task ReadNonExistentResource_ReturnsError()
    {
        var request = ResourceHelper.CreateResourceReadRequest(1, "file://nonexistent/resource.txt");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        var root = JsonDocument.Parse(ResourceHelper.ExtractJsonFromSSE(response)).RootElement;
        Assert.True(root.TryGetProperty("jsonrpc", out _));
        Assert.True(root.TryGetProperty("error", out var errorElement), "Expected error response");
        Assert.True(errorElement.TryGetProperty("message", out _), "Error should contain a message");
    }

    private static JsonElement AssertSuccessfulResourceRead(string response)
    {
        Assert.NotNull(response);
        var root = JsonDocument.Parse(ResourceHelper.ExtractJsonFromSSE(response)).RootElement;
        Assert.True(root.TryGetProperty("jsonrpc", out var versionElement));
        Assert.Equal("2.0", versionElement.GetString());
        Assert.True(root.TryGetProperty("result", out var resultElement), "Expected successful result");
        Assert.True(resultElement.TryGetProperty("contents", out var contentsArray));
        return contentsArray.EnumerateArray().First();
    }
}
