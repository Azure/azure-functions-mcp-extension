// Copyright(c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol.Protocol;
using System.Globalization;
using ModelContextProtocol;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ToolTests;

/// <summary>
/// Tool invocation tests that make direct HTTP requests to the default server
/// </summary>
public class CallToolTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    private Uri AppRootEndpoint => Fixture.AppRootEndpoint ?? throw new InvalidOperationException("AppRootEndpoint is not set. Ensure the fixture is initialized properly.");

    [Fact]
    public async Task DefaultServer_GetSnippets_Success()
    {
        // First save a snippet
        var snippetName = "default-retrieval-test";
        var snippetContent = "test content for snippet";
        var saveRequest = CallToolHelper.CreateToolCallRequest(1, "savesnippet", CallToolHelper.CreateSaveSnippetArguments(snippetName, snippetContent));
        var saveResponse = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, saveRequest, TestOutputHelper);
        TestOutputHelper.WriteLine($"SaveSnippet response: {saveResponse}");

        // Then retrieve it
        var getRequest = CallToolHelper.CreateToolCallRequest(2, "getsnippets", CallToolHelper.CreateGetSnippetArguments(snippetName));
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, getRequest, TestOutputHelper);

        TestOutputHelper.WriteLine($"GetSnippets response: {response}");

        Assert.NotNull(response);

        var result = CallToolResultHelper.ParseSseToCallToolResult(response, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(result);

        // Verify text content is present (for backwards compatibility)
        Assert.Single(result.Content);
        var textBlock = Assert.IsType<TextContentBlock>(result.Content[0]);
        Assert.Contains(snippetName, textBlock.Text);
        Assert.Contains(snippetContent, textBlock.Text);

        // Verify structured content is present (Snippet class is decorated with [McpContent])
        Assert.NotNull(result.StructuredContent);
        var structuredContent = result.StructuredContent.ToString();
        Assert.Contains("name", structuredContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(snippetName, structuredContent);
        Assert.Contains("content", structuredContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(snippetContent, structuredContent);
    }

    [Fact]
    public async Task DefaultServer_SearchSnippets_Success()
    {
        // First save some snippets
        var snippet1Name = "search-test-1";
        var snippet1Content = "function searchTest1() { return 'test1'; }";
        var snippet2Name = "search-test-2";
        var snippet2Content = "function searchTest2() { return 'test2'; }";

        var saveRequest1 = CallToolHelper.CreateToolCallRequest(4, "savesnippet", CallToolHelper.CreateSaveSnippetArguments(snippet1Name, snippet1Content));
        var saveRequest2 = CallToolHelper.CreateToolCallRequest(5, "savesnippet", CallToolHelper.CreateSaveSnippetArguments(snippet2Name, snippet2Content));

        await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, saveRequest1, TestOutputHelper);
        await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, saveRequest2, TestOutputHelper);

        // Search for snippets
        var searchRequest = CallToolHelper.CreateToolCallRequest(6, "searchsnippets", CallToolHelper.CreateSearchSnippetsArguments("search-test", false));
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, searchRequest, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("searchTest1", response);
        Assert.Contains("searchTest2", response);
        TestOutputHelper.WriteLine($"SearchSnippets response: {response}");
    }

    [Fact]
    public async Task DefaultServer_InvalidTool_ReturnsError()
    {
        var request = CallToolHelper.CreateToolCallRequest(7, "nonexistent-tool", new { someParam = "test" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        TestOutputHelper.WriteLine($"Response received: {response}");
        Assert.Contains("error", response);
        Assert.Contains("Unknown tool", response);
    }

    [Fact]
    public async Task DefaultServer_MultipleSequentialRequests_Success()
    {
        for (int i = 1; i <= 3; i++)
        {
            var request = CallToolHelper.CreateToolCallRequest(7 + i, "savesnippet",
                CallToolHelper.CreateSaveSnippetArguments($"sequential-test-{i}", $"const sequentialTest{i} = {i};"));

            var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);
            Assert.NotNull(response);
        }

        TestOutputHelper.WriteLine("Sequential requests completed successfully");
    }

    [Fact]
    public async Task DefaultServer_HappyFunction_Success()
    {
        // Test calling HappyFunction on Default server (TestAppIsolated)
        var request = CallToolHelper.CreateToolCallRequest(2, "HappyFunction", new
        {
            name = "DefaultTestUser",
            job = "FullTime",
            age = 28,
            isHappy = true,
            attributes = new[] { "diligent", "team-player", "detail-oriented" },
            numbers = new[] { 7, 14, 21 }
        });

        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        TestOutputHelper.WriteLine($"Default HappyFunction response: {response}");
        Assert.NotNull(response);
        Assert.Contains("Hello, DefaultTestUser!", response);
        Assert.Contains("FullTime", response);
    }

    [Fact]
    public async Task DefaultServer_SingleArgumentFunction_Success()
    {
        // Test calling SingleArgumentFunction on Default server
        var request = CallToolHelper.CreateToolCallRequest(3, "SingleArgumentFunction", new
        {
            argument = "default-server-test-argument"
        });

        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("default-server-test-argument", response);
        TestOutputHelper.WriteLine($"Default SingleArgumentFunction response: {response}");
    }

    [Fact]
    public async Task DefaultServer_SingleArgumentWithDefaultFunction_Success()
    {
        // Test calling SingleArgumentWithDefaultFunction with argument
        var requestWithArg = CallToolHelper.CreateToolCallRequest(4, "SingleArgumentWithDefaultFunction", new
        {
            argument = "custom-argument"
        });

        var responseWithArg = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, requestWithArg, TestOutputHelper);

        Assert.NotNull(responseWithArg);
        Assert.Contains("custom-argument", responseWithArg);
        TestOutputHelper.WriteLine($"Default SingleArgumentWithDefaultFunction (with arg) response: {responseWithArg}");

        // Test calling it without argument (should use default)
        var requestWithoutArg = CallToolHelper.CreateToolCallRequest(5, "SingleArgumentWithDefaultFunction", new { });

        var responseWithoutArg = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, requestWithoutArg, TestOutputHelper);

        Assert.NotNull(responseWithoutArg);
        TestOutputHelper.WriteLine($"Default SingleArgumentWithDefaultFunction (default) response: {responseWithoutArg}");
        Assert.Contains("(no-argument)", responseWithoutArg);
    }

    [Fact]
    public async Task DefaultServer_BirthdayTracker_Success()
    {
        // Test calling BirthdayTracker on Default server (TestAppIsolated)
        var request = CallToolHelper.CreateToolCallRequest(10, "BirthdayTracker", new
        {
            userId = "9fe500ac-e415-4c59-a766-d6378ebd7acd",
            birthday = "1995-10-13 14:45:32Z"
        });

        var expectedDate = DateTime.Parse("1995-10-13 14:45:32Z", null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal)
            .ToUniversalTime()
            .ToString("o", CultureInfo.InvariantCulture);

        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        TestOutputHelper.WriteLine($"Default BirthdayTracker response: {response}");
        Assert.NotNull(response);
        Assert.Contains("9fe500ac-e415-4c59-a766-d6378ebd7acd", response);
        Assert.Contains(expectedDate, response);
    }

    [Fact]
    public async Task DefaultServer_RenderImage_Success()
    {
        var imageDataPath = GetTestDataFilePath("image-base64.txt");
        var data = await File.ReadAllTextAsync(imageDataPath, TestContext.Current.CancellationToken);

        // Test calling RenderImage on Default server (TestAppIsolated)
        var request = CallToolHelper.CreateToolCallRequest(10, "RenderImage", new
        {
            data,
            mimeType = "image/jpeg"
        });

        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        TestOutputHelper.WriteLine($"Default RenderImage response: {response}");

        var result = CallToolResultHelper.ParseSseToCallToolResult(response, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(result);
        Assert.Single(result.Content);

        Assert.Contains(result.Content, block => block is ImageContentBlock imageBlock
            && imageBlock.MimeType == "image/jpeg"
            && imageBlock.Data.StartsWith(data.Substring(0, 20)));
    }

    [Fact]
    public async Task DefaultServer_MultiContentTypeFunction_Success()
    {
        var imageDataPath = GetTestDataFilePath("image-base64.txt");
        var data = await File.ReadAllTextAsync(imageDataPath, TestContext.Current.CancellationToken);

        // Test calling MultiContentTypeFunction on Default server (TestAppIsolated)
        var request = CallToolHelper.CreateToolCallRequest(10, "MultiContentTypeFunction", new
        {
            data,
            mimeType = "image/jpeg"
        });

        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        TestOutputHelper.WriteLine($"Default MultiContentTypeFunction response: {response}");

        var result = CallToolResultHelper.ParseSseToCallToolResult(response, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal(3, result.Content.Count);

        Assert.Contains(result.Content, block => block is TextContentBlock textBlock && textBlock.Text == "Here is an image for you!");
        Assert.Contains(result.Content, block => block is ResourceLinkBlock linkBlock && linkBlock.Uri == "https://www.google.com/");
        Assert.Contains(result.Content, block => block is ImageContentBlock imageBlock && imageBlock.MimeType == "image/jpeg" && imageBlock.Data.StartsWith(data.Substring(0, 20)));
    }

    [Fact]
    public async Task DefaultServer_GetImageWithMetadata_ReturnsStructuredContent()
    {
        // Test calling GetImageWithMetadata on Default server (TestAppIsolated)
        var request = CallToolHelper.CreateToolCallRequest(11, "GetImageWithMetadata", new { });

        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        TestOutputHelper.WriteLine($"Default GetImageWithMetadata response: {response}");

        var result = CallToolResultHelper.ParseSseToCallToolResult(response, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(result);

        // Verify content blocks are present
        Assert.Equal(2, result.Content.Count);
        Assert.Contains(result.Content, block => block is TextContentBlock);
        Assert.Contains(result.Content, block => block is ImageContentBlock imageBlock && imageBlock.MimeType == "image/png");

        // Verify structured content is present and contains expected metadata
        Assert.NotNull(result.StructuredContent);
        var structuredContent = result.StructuredContent.ToString();
        Assert.Contains("ImageId", structuredContent);
        Assert.Contains("icon", structuredContent);
        Assert.Contains("Format", structuredContent);
        Assert.Contains("png", structuredContent);
        Assert.Contains("Tags", structuredContent);
        Assert.Contains("functions", structuredContent);
    }

    private static string GetTestDataFilePath(string fileName)
    {
        // Use the test assembly location to find test data files
        var assemblyLocation = Path.GetDirectoryName(typeof(CallToolTests).Assembly.Location) 
            ?? throw new InvalidOperationException("Could not determine assembly location");
        
        var testDataPath = Path.Combine(assemblyLocation, "TestData", fileName);
        
        if (!File.Exists(testDataPath))
        {
            throw new FileNotFoundException($"Test data file not found at: {testDataPath}", fileName);
        }
        
        return testDataPath;
    }
}
