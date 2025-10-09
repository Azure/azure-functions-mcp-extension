// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Xunit.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.InvocationTests;

/// <summary>
/// Tool invocation tests that make direct HTTP requests to the default server
/// </summary>
public class ServerToolInvocationTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper) : IClassFixture<DefaultProjectFixture>
{
    private readonly DefaultProjectFixture _fixture = fixture;
    protected readonly ITestOutputHelper TestOutputHelper = testOutputHelper;

    private Uri AppRootEndpoint => _fixture.AppRootEndpoint ?? throw new InvalidOperationException("AppRootEndpoint is not set. Ensure the fixture is initialized properly.");

    [Fact]
    public async Task DefaultServer_GetSnippets_Success()
    {
        // First save a snippet
        var saveRequest = ServerToolInvocationHelper.CreateToolCallRequest(1, "savesnippet", ServerToolInvocationHelper.CreateSaveSnippetArguments("default-retrieval-test", "test"));
        var saveResponse = await ServerToolInvocationHelper.MakeToolCallRequest(AppRootEndpoint, saveRequest, TestOutputHelper);
        TestOutputHelper.WriteLine($"SaveSnippet response: {saveResponse}");

        // Then retrieve it
        var getRequest = ServerToolInvocationHelper.CreateToolCallRequest(2, "getsnippets", ServerToolInvocationHelper.CreateGetSnippetArguments("default-retrieval-test"));
        var response = await ServerToolInvocationHelper.MakeToolCallRequest(AppRootEndpoint, getRequest, TestOutputHelper);

        TestOutputHelper.WriteLine($"GetSnippets response: {response}");

        Assert.NotNull(response);
        Assert.Contains("test", response);
    }

    [Fact]
    public async Task DefaultServer_SearchSnippets_Success()
    {
        // First save some snippets
        var snippet1Name = "search-test-1";
        var snippet1Content = "function searchTest1() { return 'test1'; }";
        var snippet2Name = "search-test-2";
        var snippet2Content = "function searchTest2() { return 'test2'; }";

        var saveRequest1 = ServerToolInvocationHelper.CreateToolCallRequest(4, "savesnippet", ServerToolInvocationHelper.CreateSaveSnippetArguments(snippet1Name, snippet1Content));
        var saveRequest2 = ServerToolInvocationHelper.CreateToolCallRequest(5, "savesnippet", ServerToolInvocationHelper.CreateSaveSnippetArguments(snippet2Name, snippet2Content));

        await ServerToolInvocationHelper.MakeToolCallRequest(AppRootEndpoint, saveRequest1, TestOutputHelper);
        await ServerToolInvocationHelper.MakeToolCallRequest(AppRootEndpoint, saveRequest2, TestOutputHelper);

        // Search for snippets
        var searchRequest = ServerToolInvocationHelper.CreateToolCallRequest(6, "searchsnippets", ServerToolInvocationHelper.CreateSearchSnippetsArguments("search-test", false));
        var response = await ServerToolInvocationHelper.MakeToolCallRequest(AppRootEndpoint, searchRequest, TestOutputHelper);
        
        Assert.NotNull(response);
        Assert.Contains("searchTest1", response);
        Assert.Contains("searchTest2", response);
        TestOutputHelper.WriteLine($"SearchSnippets response: {response}");
    }

    [Fact]
    public async Task DefaultServer_InvalidTool_ReturnsError()
    {
        var request = ServerToolInvocationHelper.CreateToolCallRequest(7, "nonexistent-tool", new { someParam = "test" });
        var response = await ServerToolInvocationHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        TestOutputHelper.WriteLine($"Response received: {response}");
        Assert.Contains("error", response);
        Assert.Contains("Unknown tool", response);
    }

    [Fact]
    public async Task DefaultServer_MultipleSequentialRequests_Success()
    {
        for (int i = 1; i <= 3; i++)
        {
            var request = ServerToolInvocationHelper.CreateToolCallRequest(7 + i, "savesnippet", 
                ServerToolInvocationHelper.CreateSaveSnippetArguments($"sequential-test-{i}", $"const sequentialTest{i} = {i};"));

            var response = await ServerToolInvocationHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);
            Assert.NotNull(response);
        }

        TestOutputHelper.WriteLine("Sequential requests completed successfully");
    }

    [Fact]
    public async Task DefaultServer_HappyFunction_Success()
    {
        // Test calling HappyFunction on Default server (TestAppIsolated)
        var request = ServerToolInvocationHelper.CreateToolCallRequest(2, "HappyFunction", new
        {
            name = "DefaultTestUser",
            job = "QA Engineer",
            age = 28,
            isHappy = true
        });

        var response = await ServerToolInvocationHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);
        
        Assert.NotNull(response);
        Assert.Contains("Hello, DefaultTestUser!", response);
        Assert.Contains("QA Engineer", response);
        TestOutputHelper.WriteLine($"Default HappyFunction response: {response}");
    }

    [Fact]
    public async Task DefaultServer_SingleArgumentFunction_Success()
    {
        // Test calling SingleArgumentFunction on Default server
        var request = ServerToolInvocationHelper.CreateToolCallRequest(3, "SingleArgumentFunction", new
        {
            argument = "default-server-test-argument"
        });

        var response = await ServerToolInvocationHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);
        
        Assert.NotNull(response);
        Assert.Contains("default-server-test-argument", response);
        TestOutputHelper.WriteLine($"Default SingleArgumentFunction response: {response}");
    }

    [Fact]
    public async Task DefaultServer_SingleArgumentWithDefaultFunction_Success()
    {
        // Test calling SingleArgumentWithDefaultFunction with argument
        var requestWithArg = ServerToolInvocationHelper.CreateToolCallRequest(4, "SingleArgumentWithDefaultFunction", new
        {
            argument = "custom-argument"
        });

        var responseWithArg = await ServerToolInvocationHelper.MakeToolCallRequest(AppRootEndpoint, requestWithArg, TestOutputHelper);
        
        Assert.NotNull(responseWithArg);
        Assert.Contains("custom-argument", responseWithArg);
        TestOutputHelper.WriteLine($"Default SingleArgumentWithDefaultFunction (with arg) response: {responseWithArg}");

        // Test calling it without argument (should use default)
        var requestWithoutArg = ServerToolInvocationHelper.CreateToolCallRequest(5, "SingleArgumentWithDefaultFunction", new { });

        var responseWithoutArg = await ServerToolInvocationHelper.MakeToolCallRequest(AppRootEndpoint, requestWithoutArg, TestOutputHelper);
        
        Assert.NotNull(responseWithoutArg);
        TestOutputHelper.WriteLine($"Default SingleArgumentWithDefaultFunction (default) response: {responseWithoutArg}");
        Assert.Contains("(no-argument)", responseWithoutArg);
    }
}
