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
    public async Task ListPrompts_ReturnsExpectedCount(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var prompts = await client.ListPromptsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(prompts);
        Assert.Equal(6, prompts.Count);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListPrompts_ContainsAllPrompts(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var prompts = await client.ListPromptsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(prompts, p => p.Name == "code_review");
        Assert.Contains(prompts, p => p.Name == "summarize");
        Assert.Contains(prompts, p => p.Name == "no_args_prompt");
        Assert.Contains(prompts, p => p.Name == "all_optional_args_prompt");
        Assert.Contains(prompts, p => p.Name == "metadata_prompt");
        Assert.Contains(prompts, p => p.Name == "fluent_prompt");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListPrompts_CodeReview_HasExpectedMetadata(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var prompts = await client.ListPromptsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var codeReview = prompts.FirstOrDefault(p => p.Name == "code_review");
        Assert.NotNull(codeReview);
        Assert.Equal("Code Review", codeReview.Title);
        Assert.Equal("Generates a code review prompt for the given code snippet", codeReview.Description);

        var args = codeReview.ProtocolPrompt.Arguments;
        Assert.NotNull(args);
        Assert.Equal(2, args.Count);

        var codeArg = args.FirstOrDefault(a => a.Name == "code");
        Assert.NotNull(codeArg);
        Assert.Equal("The code to review", codeArg.Description);
        Assert.True(codeArg.Required);

        var langArg = args.FirstOrDefault(a => a.Name == "language");
        Assert.NotNull(langArg);
        Assert.Equal("The programming language", langArg.Description);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListPrompts_Summarize_HasSingleRequiredArg(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var prompts = await client.ListPromptsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var summarize = prompts.FirstOrDefault(p => p.Name == "summarize");
        Assert.NotNull(summarize);
        Assert.Equal("Summarize Text", summarize.Title);

        var args = summarize.ProtocolPrompt.Arguments;
        Assert.NotNull(args);
        Assert.Single(args);
        Assert.Equal("text", args[0].Name);
        Assert.True(args[0].Required);
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListPrompts_NoArgsPrompt_HasNoArguments(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var prompts = await client.ListPromptsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var noArgs = prompts.FirstOrDefault(p => p.Name == "no_args_prompt");
        Assert.NotNull(noArgs);
        Assert.Equal("No Arguments Prompt", noArgs.Title);

        var args = noArgs.ProtocolPrompt.Arguments;
        Assert.True(args is null || args.Count == 0, "No-args prompt should have no arguments");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListPrompts_AllOptionalArgsPrompt_HasOptionalArgs(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var prompts = await client.ListPromptsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var allOptional = prompts.FirstOrDefault(p => p.Name == "all_optional_args_prompt");
        Assert.NotNull(allOptional);
        Assert.Equal("All Optional Arguments", allOptional.Title);

        var args = allOptional.ProtocolPrompt.Arguments;
        Assert.NotNull(args);
        Assert.Equal(2, args.Count);

        // All arguments should be non-required
        Assert.All(args, a => Assert.False(a.Required, $"Argument '{a.Name}' should not be required"));
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task ListPrompts_FluentPrompt_HasBuilderDefinedArgs(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var prompts = await client.ListPromptsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var fluent = prompts.FirstOrDefault(p => p.Name == "fluent_prompt");
        Assert.NotNull(fluent);
        Assert.Equal("Fluent Prompt", fluent.Title);

        var args = fluent.ProtocolPrompt.Arguments;
        Assert.NotNull(args);

        var queryArg = args.FirstOrDefault(a => a.Name == "query");
        Assert.NotNull(queryArg);
        Assert.Equal("The search query to process", queryArg.Description);
        Assert.True(queryArg.Required);
    }
}
