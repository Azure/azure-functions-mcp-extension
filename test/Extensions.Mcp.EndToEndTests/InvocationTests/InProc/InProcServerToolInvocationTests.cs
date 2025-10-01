// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Xunit.Abstractions;
using static Extensions.Mcp.EndToEndTests.Fixtures.McpEndToEndProjectFixtures;

namespace Microsoft.Azure.Functions.Extensions.Mcp.EndToEndTests.InvocationTests.InProc;

/// <summary>
/// InProc server tool invocation tests that make direct HTTP requests to TestApp
/// </summary>
public class InProcServerToolInvocationTests(InProcProjectFixture fixture, ITestOutputHelper testOutputHelper) 
    : ServerToolInvocationBase(fixture, testOutputHelper), IClassFixture<InProcProjectFixture>
{
    [Fact(Skip = "Need to investigate why this test fails")]
    public async Task InProcServer_GetSnippets_Success()
    {
        await AssertGetSnippetsSuccess("inproc-retrieval-test", "test");
    }

    [Fact(Skip = "Need to investigate why this test fails")]
    public async Task InProcServer_SearchSnippets_Success()
    {
        await AssertSearchSnippetsSuccess();
    }

    [Fact]
    public async Task InProcServer_InvalidTool_ReturnsError()
    {
        await AssertInvalidToolReturnsError();
    }

    [Fact]
    public async Task InProcServer_MultipleSequentialRequests_Success()
    {
        await AssertMultipleSequentialRequestsSuccess();
    }
}
