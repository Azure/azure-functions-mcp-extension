// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.EndToEndTests.InvocationTests;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEndTests.Fixtures.McpEndToEndProjectFixtures;

namespace Extensions.Mcp.EndToEndTests.InvocationTests.Default;

/// <summary>
/// Default server tool invocation tests that make direct HTTP requests to TestAppIsolated
/// </summary>
public class DefaultServerToolInvocationTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper) 
    : ServerToolInvocationBase(fixture, testOutputHelper), IClassFixture<DefaultProjectFixture>
{

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

        // Test calling SingleArgumentWithDefaultFunction without argument (should use default)
        var requestWithoutArg = CreateToolCallRequest(5, "SingleArgumentWithDefaultFunction", new { });

        var responseWithoutArg = await MakeToolCallRequest(requestWithoutArg);
        
        Assert.NotNull(responseWithoutArg);
        TestOutputHelper.WriteLine($"Default SingleArgumentWithDefaultFunction responses: With arg: {responseWithArg}, Without arg: {responseWithoutArg}");
    }

    [Fact]
    public async Task DefaultServer_ComplexWorkflow_Success()
    {
        // Test a complex workflow using multiple tools
        
        // 1. Use HappyFunction to get a greeting
        var greetingRequest = CreateToolCallRequest(11, "HappyFunction", new
        {
            name = "WorkflowUser",
            job = "Tester",
            age = 25,
            isHappy = true
        });

        var greetingResponse = await MakeToolCallRequest(greetingRequest);
        Assert.Contains("WorkflowUser", greetingResponse);

        // 2. Save the greeting as a snippet
        var saveRequest = CreateToolCallRequest(12, "savesnippet", CreateSaveSnippetArguments("workflow-greeting", "test"));
        await MakeToolCallRequest(saveRequest);

        // 3. Retrieve the saved snippet
        var retrieveRequest = CreateToolCallRequest(13, "getsnippets", CreateGetSnippetArguments("workflow-greeting"));
        var retrieveResponse = await MakeToolCallRequest(retrieveRequest);
        Assert.Contains("test", retrieveResponse);

        // 4. Use SingleArgumentFunction to echo the workflow completion
        var echoRequest = CreateToolCallRequest(14, "SingleArgumentFunction", new
        {
            argument = "Complex workflow completed successfully"
        });

        var echoResponse = await MakeToolCallRequest(echoRequest);
        Assert.Contains("workflow completed", echoResponse);

        TestOutputHelper.WriteLine("Default Complex Workflow completed successfully");
    }
}
