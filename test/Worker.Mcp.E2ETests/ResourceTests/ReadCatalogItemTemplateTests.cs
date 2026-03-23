// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ResourceTests;

/// <summary>
/// Tests for reading resources using a URI template with literal text (not a URI delimiter)
/// between template expressions: store://catalog/{category}items{tag}
/// </summary>
public class ReadCatalogItemTemplateTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    private Uri AppRootEndpoint => Fixture.AppRootEndpoint ?? throw new InvalidOperationException("AppRootEndpoint is not set. Ensure the fixture is initialized properly.");

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListResourceTemplates_ReturnsCatalogItemTemplate(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var templates = await client.ListResourceTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken);

        var catalogTemplate = templates.FirstOrDefault(t => t.UriTemplate == "store://catalog/{category}items{tag}");
        Assert.NotNull(catalogTemplate);
        Assert.Equal("catalogItem", catalogTemplate.Name);
        Assert.Equal("Catalog item lookup by category and tag", catalogTemplate.Description);

        TestOutputHelper.WriteLine($"Found template: {catalogTemplate.UriTemplate}");
    }

    [Fact]
    public async Task ReadCatalogItem_ExtractsParametersFromLiteralSeparatedTemplate()
    {
        // "store://catalog/booksitemsfiction" should parse as category=books, tag=fiction
        var request = ResourceHelper.CreateResourceReadRequest(1, "store://catalog/booksitemsfiction");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        TestOutputHelper.WriteLine($"Response: {response}");

        Assert.NotNull(response);
        var jsonString = ResourceHelper.ExtractJsonFromSSE(response);
        var jsonResponse = JsonDocument.Parse(jsonString);
        var root = jsonResponse.RootElement;

        Assert.True(root.TryGetProperty("jsonrpc", out var versionElement));
        Assert.Equal("2.0", versionElement.GetString());
        Assert.True(root.TryGetProperty("result", out var resultElement), "Expected successful result");
        Assert.True(resultElement.TryGetProperty("contents", out var contentsArray));

        var contents = contentsArray.EnumerateArray().FirstOrDefault();
        Assert.True(contents.TryGetProperty("text", out var textElement), "Expected text content");

        var text = textElement.GetString();
        Assert.NotNull(text);

        // The function echoes back the extracted parameters as JSON
        var echoedParams = JsonDocument.Parse(text);
        Assert.Equal("books", echoedParams.RootElement.GetProperty("category").GetString());
        Assert.Equal("fiction", echoedParams.RootElement.GetProperty("tag").GetString());

        TestOutputHelper.WriteLine($"Extracted: category=books, tag=fiction");
    }

    [Fact]
    public async Task ReadCatalogItem_DifferentValues_ExtractsCorrectly()
    {
        var request = ResourceHelper.CreateResourceReadRequest(1, "store://catalog/electronicsitemsgaming");
        var response = await ResourceHelper.MakeResourceRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        var jsonString = ResourceHelper.ExtractJsonFromSSE(response);
        var jsonResponse = JsonDocument.Parse(jsonString);
        var root = jsonResponse.RootElement;

        Assert.True(root.TryGetProperty("result", out var resultElement), "Expected successful result");
        Assert.True(resultElement.TryGetProperty("contents", out var contentsArray));

        var contents = contentsArray.EnumerateArray().FirstOrDefault();
        Assert.True(contents.TryGetProperty("text", out var textElement));

        var echoedParams = JsonDocument.Parse(textElement.GetString()!);
        Assert.Equal("electronics", echoedParams.RootElement.GetProperty("category").GetString());
        Assert.Equal("gaming", echoedParams.RootElement.GetProperty("tag").GetString());

        TestOutputHelper.WriteLine($"Extracted: category=electronics, tag=gaming");
    }
}
