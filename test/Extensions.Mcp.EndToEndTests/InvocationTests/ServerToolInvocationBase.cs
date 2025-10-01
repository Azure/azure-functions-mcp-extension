// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.EndToEndTests.Fixtures;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Extensions.Mcp.EndToEndTests.InvocationTests;

/// <summary>
/// Base class for server tool invocation tests that make direct HTTP requests
/// </summary>
public abstract class ServerToolInvocationBase
{
    private readonly McpEndToEndFixtureBase _fixture;
    protected readonly ITestOutputHelper TestOutputHelper;
    private Uri? _cachedEndpoint;

    protected ServerToolInvocationBase(McpEndToEndFixtureBase fixture, ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;
        TestOutputHelper = testOutputHelper;
    }

    /// <summary>
    /// Gets the MCP endpoint for the current server
    /// </summary>
    private Uri GetMcpEndpoint()
    {
        if (_cachedEndpoint != null)
            return _cachedEndpoint;

        // Determine the correct port based on fixture type
        var baseUri = new Uri("http://localhost:7071"); // Default for InProc
        
        _cachedEndpoint = new Uri(baseUri, "/runtime/webhooks/mcp");
        return _cachedEndpoint;
    }

    /// <summary>
    /// Determines if this is a Default server based on the fixture type
    /// </summary>
    protected bool IsDefaultServer => _fixture.GetType().Name.Contains("Default");

    /// <summary>
    /// Makes a tool call request to the server endpoint
    /// </summary>
    protected async Task<string> MakeToolCallRequest(object request)
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
    protected static object CreateToolCallRequest(int id, string toolName, object arguments)
    {
        return new
        {
            jsonrpc = "2.0",
            id,
            method = "tools/call",
            @params = new
            {
                name = toolName,  // Fixed: was "Name" should be "name"
                arguments
            }
        };
    }

    /// <summary>
    /// Creates SaveSnippet arguments appropriate for the server type
    /// </summary>
    protected object CreateSaveSnippetArguments(string name, string content)
    {
        if (IsDefaultServer)
        {
            // Default server expects POCO with Name and Content properties
            return new { Name = name, Content = content };
        }
        else
        {
            // InProc server expects name and snippet properties
            return new { snippetname = name, snippet = content };
        }
    }

    /// <summary>
    /// Creates GetSnippets arguments appropriate for the server type
    /// </summary>
    protected object CreateGetSnippetArguments(string snippetName)
    {
        // Both servers use the same parameter name for getting snippets
        return new { snippetname = snippetName };
    }

    /// <summary>
    /// Creates SearchSnippets arguments appropriate for the server type
    /// </summary>
    protected object CreateSearchSnippetsArguments(string pattern, bool caseSensitive = false)
    {
        if (IsDefaultServer)
        {
            // Default server expects POCO with Pattern and CaseSensitive properties
            return new { Pattern = pattern, CaseSensitive = caseSensitive };
        }
        else
        {
            // InProc server expects pattern and caseSensitive properties
            return new { pattern = pattern, caseSensitive = caseSensitive };
        }
    }

    /// <summary>
    /// Base test for GetSnippets functionality - works on both server types
    /// </summary>
    protected async Task AssertGetSnippetsSuccess(string testName, string expectedContent)
    {
        // First save a snippet
        var saveRequest = CreateToolCallRequest(1, "savesnippet", CreateSaveSnippetArguments(testName, expectedContent));
        var saveResponse = await MakeToolCallRequest(saveRequest);
        TestOutputHelper.WriteLine($"GetSnippets response: {saveResponse}");

        // Then retrieve it
        var getRequest = CreateToolCallRequest(2, "getsnippets", CreateGetSnippetArguments(testName));
        var response = await MakeToolCallRequest(getRequest);

        TestOutputHelper.WriteLine($"GetSnippets response: {response}");

        Assert.NotNull(response);
        Assert.Contains(expectedContent, response);
    }

    /// <summary>
    /// Base test for SearchSnippets functionality - works on both server types
    /// </summary>
    protected async Task AssertSearchSnippetsSuccess()
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

        // Search for snippets - use different tool name based on server type
        var searchToolName = IsDefaultServer ? "searchsnippets" : "searchSnippets";
        var searchRequest = CreateToolCallRequest(6, searchToolName, CreateSearchSnippetsArguments("search-test", false));
        var response = await MakeToolCallRequest(searchRequest);
        
        Assert.NotNull(response);
        Assert.Contains("searchTest1", response);
        Assert.Contains("searchTest2", response);
        TestOutputHelper.WriteLine($"SearchSnippets response: {response}");
    }

    /// <summary>
    /// Base test for invalid tool calls - works on both server types
    /// </summary>
    protected async Task AssertInvalidToolReturnsError()
    {
        var request = CreateToolCallRequest(7, "nonexistent-tool", new { someParam = "test" });
        var response = await MakeToolCallRequest(request);

        TestOutputHelper.WriteLine($"Response received: {response}");
        Assert.Contains("error", response);
        Assert.Contains("Unknown tool", response);
    }

    /// <summary>
    /// Base test for multiple sequential requests - works on both server types
    /// </summary>
    protected async Task AssertMultipleSequentialRequestsSuccess()
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
