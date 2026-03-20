// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ResourceTests;

/// <summary>
/// Tests for reading resources using URI templates with parameters
/// </summary>
public class ReadResourceTemplateTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    private Uri AppRootEndpoint => Fixture.AppRootEndpoint ?? throw new InvalidOperationException("AppRootEndpoint is not set. Ensure the fixture is initialized properly.");

    [Fact]
    public async Task DefaultServer_ReadResourceTemplate_WithParameter_Success()
    {
        // Read a resource using the template with a parameter value
        var request = ResourceHelper.CreateResourceReadRequest(1, "user://profile/bob");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        TestOutputHelper.WriteLine($"ReadResource response: {response}");

        Assert.NotNull(response);
        var jsonString = ResourceHelper.ExtractJsonFromSSE(response);
        var jsonResponse = JsonDocument.Parse(jsonString);
        var root = jsonResponse.RootElement;

        // Response should be valid JSON-RPC with result
        Assert.True(root.TryGetProperty("jsonrpc", out var versionElement));
        Assert.Equal("2.0", versionElement.GetString());

        // Should have result (not error)
        Assert.True(root.TryGetProperty("result", out var resultElement), "Expected successful result");
        Assert.True(resultElement.TryGetProperty("contents", out var contentsArray));

        var contents = contentsArray.EnumerateArray().FirstOrDefault();
        Assert.True(contents.TryGetProperty("text", out var textElement) || contents.TryGetProperty("blob", out _),
            "Expected text or blob content");

        if (contents.TryGetProperty("text", out textElement))
        {
            var text = textElement.GetString();
            Assert.NotNull(text);
            TestOutputHelper.WriteLine($"Resource content: {text}");
        }
    }

    [Fact]
    public async Task DefaultServer_ReadResourceTemplate_DifferentParameters_ReturnsDifferentContent()
    {
        // Read with "bob" parameter
        var bobRequest = ResourceHelper.CreateResourceReadRequest(1, "user://profile/bob");
        var bobResponse = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, bobRequest, TestOutputHelper);

        // Read with "alice" parameter
        var aliceRequest = ResourceHelper.CreateResourceReadRequest(2, "user://profile/alice");
        var aliceResponse = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, aliceRequest, TestOutputHelper);

        TestOutputHelper.WriteLine($"Bob response: {bobResponse}");
        TestOutputHelper.WriteLine($"Alice response: {aliceResponse}");

        // Both should succeed (assuming test data exists)
        Assert.NotNull(bobResponse);
        Assert.NotNull(aliceResponse);

        var bobJson = ResourceHelper.ExtractJsonFromSSE(bobResponse);
        var aliceJson = ResourceHelper.ExtractJsonFromSSE(aliceResponse);

        // Responses should be different (different parameter values)
        // Note: This assumes bob.md and alice.md exist with different content
        Assert.NotEqual(bobJson, aliceJson);
    }
}
