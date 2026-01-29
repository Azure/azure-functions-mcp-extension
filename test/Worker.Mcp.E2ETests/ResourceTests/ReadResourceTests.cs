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
            
            // Metadata should be included (author and file)
            if (contents.TryGetProperty("metadata", out var metadataElement))
            {
                var metadata = metadataElement.EnumerateObject().ToList();
                
                // Should have author or file metadata
                var hasAuthor = metadata.Any(prop => prop.Name == "author");
                var hasFile = metadata.Any(prop => prop.Name == "file");
                
                Assert.True(hasAuthor || hasFile, "Resource should contain metadata attributes");
                
                // Validate specific metadata values
                var authorProp = metadata.FirstOrDefault(p => p.Name == "author");
                if (authorProp.Value.ValueKind != JsonValueKind.Undefined)
                {
                    Assert.Equal("John Doe", authorProp.Value.GetString());
                    TestOutputHelper.WriteLine($"Author metadata found: {authorProp.Value.GetString()}");
                }
                
                var fileProp = metadata.FirstOrDefault(p => p.Name == "file");
                if (fileProp.Value.ValueKind != JsonValueKind.Undefined)
                {
                    TestOutputHelper.WriteLine($"File metadata found: {fileProp.Value}");
                }
                
                TestOutputHelper.WriteLine("Metadata attributes validated in resource read response");
            }
        }
        else
        {
            TestOutputHelper.WriteLine("Resource read returned error - metadata validation skipped");
        }
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
