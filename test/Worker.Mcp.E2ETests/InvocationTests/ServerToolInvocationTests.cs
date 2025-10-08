// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.InvocationTests;

/// <summary>
/// Tool invocation tests that make direct HTTP requests to the default server
/// </summary>
public class ServerToolInvocationTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper) : IClassFixture<DefaultProjectFixture>
{
    private readonly DefaultProjectFixture _fixture = fixture;
    protected readonly ITestOutputHelper TestOutputHelper = testOutputHelper;
    private Uri? _cachedEndpoint;

    [Fact]
    public async Task DefaultServer_GetSnippets_Success()
    {
        await AssertGetSnippetsSuccess("default-retrieval-test", "test");
    }

    [Fact]
    public async Task DefaultServer_SearchSnippets_Success()
    {
        await AssertSearchSnippetsSuccess();
    }

    [Fact]
    public async Task DefaultServer_InvalidTool_ReturnsError()
    {
        await AssertInvalidToolReturnsError();
    }

    [Fact]
    public async Task DefaultServer_MultipleSequentialRequests_Success()
    {
        await AssertMultipleSequentialRequestsSuccess();
    }

    [Fact]
    public async Task DefaultServer_HappyFunction_Success()
    {
        // Test calling HappyFunction on Default server (TestAppIsolated)
        var request = CreateToolCallRequest(2, "HappyFunction", new
        {
            name = "DefaultTestUser",
            job = "QA Engineer",
            age = 28,
            isHappy = true
        });

        var response = await MakeToolCallRequest(request);
        
        Assert.NotNull(response);
        Assert.Contains("Hello, DefaultTestUser!", response);
        Assert.Contains("QA Engineer", response);
        TestOutputHelper.WriteLine($"Default HappyFunction response: {response}");
    }

    [Fact]
    public async Task DefaultServer_SingleArgumentFunction_Success()
    {
        // Test calling SingleArgumentFunction on Default server
        var request = CreateToolCallRequest(3, "SingleArgumentFunction", new
        {
            argument = "default-server-test-argument"
        });

        var response = await MakeToolCallRequest(request);
        
        Assert.NotNull(response);
        Assert.Contains("default-server-test-argument", response);
        TestOutputHelper.WriteLine($"Default SingleArgumentFunction response: {response}");
    }

    [Fact]
    public async Task DefaultServer_SingleArgumentWithDefaultFunction_Success()
    {
        // Test calling SingleArgumentWithDefaultFunction with argument
        var requestWithArg = CreateToolCallRequest(4, "SingleArgumentWithDefaultFunction", new
        {
            argument = "custom-argument"
        });

        var responseWithArg = await MakeToolCallRequest(requestWithArg);
        
        Assert.NotNull(responseWithArg);
        Assert.Contains("custom-argument", responseWithArg);
        TestOutputHelper.WriteLine($"Default SingleArgumentWithDefaultFunction (with arg) response: {responseWithArg}");

        // Test calling it without argument (should use default)
        var requestWithoutArg = CreateToolCallRequest(5, "SingleArgumentWithDefaultFunction", new { });

        var responseWithoutArg = await MakeToolCallRequest(requestWithoutArg);
        
        Assert.NotNull(responseWithoutArg);
        TestOutputHelper.WriteLine($"Default SingleArgumentWithDefaultFunction (default) response: {responseWithoutArg}");
        Assert.Contains("(no-argument)", responseWithoutArg);
    }

    /// <summary>
    /// Gets the MCP endpoint for the current server
    /// </summary>
    private Uri GetMcpEndpoint()
    {
        if (_cachedEndpoint != null)
            return _cachedEndpoint;

        // Use the fixture's endpoint
        if (_fixture.AppRootEndpoint is null)
        {
            throw new InvalidOperationException("AppRootEndpoint is not set. Ensure the fixture is initialized properly.");
        }
        
        _cachedEndpoint = new Uri(_fixture.AppRootEndpoint, "/runtime/webhooks/mcp");
        return _cachedEndpoint;
    }

    /// <summary>
    /// Makes a tool call request to the server endpoint
    /// </summary>
    private async Task<string> MakeToolCallRequest(object request)
    {
        using var httpClient = new HttpClient();
        
        var json = JsonSerializer.Serialize(request);
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Add required MCP headers
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/event-stream");
        
        var mcpEndpoint = GetMcpEndpoint();
        
        TestOutputHelper.WriteLine($"Making request to {mcpEndpoint}: {json}");
        
        var response = await httpClient.PostAsync(mcpEndpoint, content);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            TestOutputHelper.WriteLine($"Request failed with status {response.StatusCode}: {responseContent}");
            throw new HttpRequestException($"Request failed with status {response.StatusCode}: {responseContent}");
        }
        
        return responseContent;
    }

    /// <summary>
    /// Creates a JSON-RPC request object for tool calls
    /// </summary>
    private static object CreateToolCallRequest(int id, string toolName, object arguments)
    {
        return new
        {
            jsonrpc = "2.0",
            id,
            method = "tools/call",
            @params = new
            {
                name = toolName,
                arguments
            }
        };
    }

    /// <summary>
    /// Creates SaveSnippet arguments for the default server
    /// </summary>
    private static object CreateSaveSnippetArguments(string name, string content)
    {
        // Default server expects POCO with Name and Content properties
        return new { Name = name, Content = content };
    }

    /// <summary>
    /// Creates GetSnippets arguments for the default server
    /// </summary>
    private static object CreateGetSnippetArguments(string snippetName)
    {
        // Default server uses the same parameter name for getting snippets
        return new { snippetname = snippetName };
    }

    /// <summary>
    /// Creates SearchSnippets arguments for the default server
    /// </summary>
    private static object CreateSearchSnippetsArguments(string pattern, bool caseSensitive = false)
    {
        // Default server expects POCO with Pattern and CaseSensitive properties
        return new { Pattern = pattern, CaseSensitive = caseSensitive };
    }

    /// <summary>
    /// Base test for GetSnippets functionality
    /// </summary>
    private async Task AssertGetSnippetsSuccess(string testName, string expectedContent)
    {
        // First save a snippet
        var saveRequest = CreateToolCallRequest(1, "savesnippet", CreateSaveSnippetArguments(testName, expectedContent));
        var saveResponse = await MakeToolCallRequest(saveRequest);
        TestOutputHelper.WriteLine($"SaveSnippet response: {saveResponse}");

        // Then retrieve it
        var getRequest = CreateToolCallRequest(2, "getsnippets", CreateGetSnippetArguments(testName));
        var response = await MakeToolCallRequest(getRequest);

        TestOutputHelper.WriteLine($"GetSnippets response: {response}");

        Assert.NotNull(response);
        Assert.Contains(expectedContent, response);
    }

    /// <summary>
    /// Base test for SearchSnippets functionality
    /// </summary>
    private async Task AssertSearchSnippetsSuccess()
    {
        // First save some snippets
        var snippet1Name = "search-test-1";
        var snippet1Content = "function searchTest1() { return 'test1'; }";
        var snippet2Name = "search-test-2";
        var snippet2Content = "function searchTest2() { return 'test2'; }";

        var saveRequest1 = CreateToolCallRequest(4, "savesnippet", CreateSaveSnippetArguments(snippet1Name, snippet1Content));
        var saveRequest2 = CreateToolCallRequest(5, "savesnippet", CreateSaveSnippetArguments(snippet2Name, snippet2Content));

        await MakeToolCallRequest(saveRequest1);
        await MakeToolCallRequest(saveRequest2);

        // Search for snippets
        var searchRequest = CreateToolCallRequest(6, "searchsnippets", CreateSearchSnippetsArguments("search-test", false));
        var response = await MakeToolCallRequest(searchRequest);
        
        Assert.NotNull(response);
        Assert.Contains("searchTest1", response);
        Assert.Contains("searchTest2", response);
        TestOutputHelper.WriteLine($"SearchSnippets response: {response}");
    }

    /// <summary>
    /// Base test for invalid tool calls
    /// </summary>
    private async Task AssertInvalidToolReturnsError()
    {
        var request = CreateToolCallRequest(7, "nonexistent-tool", new { someParam = "test" });
        var response = await MakeToolCallRequest(request);

        TestOutputHelper.WriteLine($"Response received: {response}");
        Assert.Contains("error", response);
        Assert.Contains("Unknown tool", response);
    }

    /// <summary>
    /// Base test for multiple sequential requests
    /// </summary>
    private async Task AssertMultipleSequentialRequestsSuccess()
    {
        for (int i = 1; i <= 3; i++)
        {
            var request = CreateToolCallRequest(7 + i, "savesnippet", 
                CreateSaveSnippetArguments($"sequential-test-{i}", $"const sequentialTest{i} = {i};"));

            var response = await MakeToolCallRequest(request);
            Assert.NotNull(response);
        }

        TestOutputHelper.WriteLine("Sequential requests completed successfully");
    }
}
