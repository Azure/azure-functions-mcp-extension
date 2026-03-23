// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.PromptTests;

public class ListPromptTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListPrompts_ReturnsAllPrompts(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var prompts = await client.ListPromptsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(prompts);
        Assert.True(prompts.Count >= 2, $"Expected at least 2 prompts, got {prompts.Count}");

        Assert.Contains(prompts, p => p.Name == "code_review");
        Assert.Contains(prompts, p => p.Name == "summarize");

        TestOutputHelper.WriteLine($"Found {prompts.Count} prompts: {string.Join(", ", prompts.Select(p => p.Name))}");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListPrompts_ContainsExpectedMetadata(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var prompts = await client.ListPromptsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var codeReview = prompts.FirstOrDefault(p => p.Name == "code_review");
        Assert.NotNull(codeReview);
        Assert.Equal("Code Review", codeReview.Title);
        Assert.Equal("Generates a code review prompt for the given code snippet", codeReview.Description);

        // Verify arguments are included
        var codeReviewArgs = codeReview.ProtocolPrompt.Arguments;
        Assert.NotNull(codeReviewArgs);
        Assert.Equal(2, codeReviewArgs.Count);

        var codeArg = codeReviewArgs.FirstOrDefault(a => a.Name == "code");
        Assert.NotNull(codeArg);
        Assert.Equal("The code to review", codeArg.Description);
        Assert.True(codeArg.Required);

        var langArg = codeReviewArgs.FirstOrDefault(a => a.Name == "language");
        Assert.NotNull(langArg);
        Assert.Equal("The programming language", langArg.Description);

        var summarize = prompts.FirstOrDefault(p => p.Name == "summarize");
        Assert.NotNull(summarize);
        Assert.Equal("Summarize Text", summarize.Title);

        var summarizeArgs = summarize.ProtocolPrompt.Arguments;
        Assert.NotNull(summarizeArgs);
        Assert.Single(summarizeArgs);
        Assert.Equal("text", summarizeArgs[0].Name);
        Assert.True(summarizeArgs[0].Required);

        TestOutputHelper.WriteLine($"code_review: Title={codeReview.Title}, Args=[{string.Join(", ", codeReviewArgs.Select(a => a.Name))}]");
        TestOutputHelper.WriteLine($"summarize: Title={summarize.Title}, Args=[{string.Join(", ", summarizeArgs.Select(a => a.Name))}]");
    }
}
