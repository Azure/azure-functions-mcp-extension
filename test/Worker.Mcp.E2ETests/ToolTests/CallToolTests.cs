// Copyright(c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using System.Globalization;

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
        var saveRequest = CallToolHelper.CreateToolCallRequest(1, "savesnippet", CallToolHelper.CreateSaveSnippetArguments("default-retrieval-test", "test"));
        var saveResponse = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, saveRequest, TestOutputHelper);
        TestOutputHelper.WriteLine($"SaveSnippet response: {saveResponse}");

        // Then retrieve it
        var getRequest = CallToolHelper.CreateToolCallRequest(2, "getsnippets", CallToolHelper.CreateGetSnippetArguments("default-retrieval-test"));
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, getRequest, TestOutputHelper);

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
            job = "Fulltime",
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
}
