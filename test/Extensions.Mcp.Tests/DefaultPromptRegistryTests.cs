// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class DefaultPromptRegistryTests
{
    [Fact]
    public void Register_WithValidPrompt_Succeeds()
    {
        var registry = new DefaultPromptRegistry();
        var prompt = CreateTestPrompt("code_review");

        registry.Register(prompt);

        Assert.True(registry.TryGetPrompt("code_review", out var retrieved));
        Assert.Same(prompt, retrieved);
    }

    [Fact]
    public void Register_WithNullPrompt_ThrowsArgumentNullException()
    {
        var registry = new DefaultPromptRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    [Fact]
    public void Register_WithDuplicateName_ThrowsInvalidOperationException()
    {
        var registry = new DefaultPromptRegistry();
        var prompt1 = CreateTestPrompt("code_review");
        var prompt2 = CreateTestPrompt("code_review");

        registry.Register(prompt1);

        var exception = Assert.Throws<InvalidOperationException>(() => registry.Register(prompt2));
        Assert.Contains("already registered", exception.Message);
        Assert.Contains("code_review", exception.Message);
    }

    [Fact]
    public void TryGetPrompt_WithExistingPrompt_ReturnsTrue()
    {
        var registry = new DefaultPromptRegistry();
        var prompt = CreateTestPrompt("summarize");
        registry.Register(prompt);

        var result = registry.TryGetPrompt("summarize", out var retrieved);

        Assert.True(result);
        Assert.NotNull(retrieved);
        Assert.Same(prompt, retrieved);
    }

    [Fact]
    public void TryGetPrompt_WithNonExistentPrompt_ReturnsFalse()
    {
        var registry = new DefaultPromptRegistry();

        var result = registry.TryGetPrompt("nonexistent", out var retrieved);

        Assert.False(result);
        Assert.Null(retrieved);
    }

    [Fact]
    public void TryGetPrompt_IsCaseInsensitive()
    {
        var registry = new DefaultPromptRegistry();
        var prompt = CreateTestPrompt("CodeReview");
        registry.Register(prompt);

        Assert.True(registry.TryGetPrompt("codereview", out _));
        Assert.True(registry.TryGetPrompt("CODEREVIEW", out _));
        Assert.True(registry.TryGetPrompt("CodeReview", out _));
    }

    [Fact]
    public void TryGetPrompt_WithNullName_ThrowsArgumentNullException()
    {
        var registry = new DefaultPromptRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.TryGetPrompt(null!, out _));
    }

    [Fact]
    public void GetPrompts_WithNoPrompts_ReturnsEmptyCollection()
    {
        var registry = new DefaultPromptRegistry();

        var prompts = registry.GetPrompts();

        Assert.NotNull(prompts);
        Assert.Empty(prompts);
    }

    [Fact]
    public void GetPrompts_WithMultiplePrompts_ReturnsAll()
    {
        var registry = new DefaultPromptRegistry();
        var prompt1 = CreateTestPrompt("code_review");
        var prompt2 = CreateTestPrompt("summarize");
        var prompt3 = CreateTestPrompt("debug");

        registry.Register(prompt1);
        registry.Register(prompt2);
        registry.Register(prompt3);

        var prompts = registry.GetPrompts();

        Assert.Equal(3, prompts.Count);
        Assert.Contains(prompt1, prompts);
        Assert.Contains(prompt2, prompts);
        Assert.Contains(prompt3, prompts);
    }

    [Fact]
    public async Task ListPromptsAsync_WithNoPrompts_ReturnsEmptyList()
    {
        var registry = new DefaultPromptRegistry();

        var result = await registry.ListPromptsAsync();

        Assert.NotNull(result);
        Assert.NotNull(result.Prompts);
        Assert.Empty(result.Prompts);
    }

    [Fact]
    public async Task ListPromptsAsync_WithPrompts_ReturnsPromptsList()
    {
        var registry = new DefaultPromptRegistry();
        var prompt = CreateTestPrompt("code_review", title: "Code Review", description: "Reviews code quality");
        registry.Register(prompt);

        var result = await registry.ListPromptsAsync();

        Assert.NotNull(result);
        Assert.Single(result.Prompts);
        var listed = result.Prompts[0];
        Assert.Equal("code_review", listed.Name);
        Assert.Equal("Code Review", listed.Title);
        Assert.Equal("Reviews code quality", listed.Description);
    }

    [Fact]
    public async Task ListPromptsAsync_WithArguments_IncludesArguments()
    {
        var registry = new DefaultPromptRegistry();
        var arguments = new List<PromptArgument>
        {
            new() { Name = "code", Description = "The code to review", Required = true },
            new() { Name = "style", Description = "Review style" }
        };
        var prompt = CreateTestPrompt("code_review", arguments: arguments);
        registry.Register(prompt);

        var result = await registry.ListPromptsAsync();

        var listed = result.Prompts[0];
        Assert.NotNull(listed.Arguments);
        Assert.Equal(2, listed.Arguments.Count);
        Assert.Equal("code", listed.Arguments[0].Name);
        Assert.True(listed.Arguments[0].Required);
        Assert.Equal("style", listed.Arguments[1].Name);
    }

    [Fact]
    public async Task ListPromptsAsync_WithMetadata_IncludesMetaJsonObject()
    {
        var registry = new DefaultPromptRegistry();
        var metadata = new Dictionary<string, object?>
        {
            ["category"] = "development",
            ["version"] = "1.0"
        };
        var prompt = CreateTestPrompt("code_review", metadata: metadata);
        registry.Register(prompt);

        var result = await registry.ListPromptsAsync();

        var listed = result.Prompts[0];
        Assert.NotNull(listed.Meta);
        Assert.Equal(2, listed.Meta.Count);
        Assert.Equal("development", listed.Meta["category"]?.GetValue<string>());
        Assert.Equal("1.0", listed.Meta["version"]?.GetValue<string>());
    }

    [Fact]
    public async Task ListPromptsAsync_WithNullMetadata_ExcludesMeta()
    {
        var registry = new DefaultPromptRegistry();
        var prompt = CreateTestPrompt("code_review", metadata: null);
        registry.Register(prompt);

        var result = await registry.ListPromptsAsync();

        var listed = result.Prompts[0];
        Assert.Null(listed.Meta);
    }

    [Fact]
    public async Task ListPromptsAsync_WithEmptyMetadata_ExcludesMeta()
    {
        var registry = new DefaultPromptRegistry();
        var prompt = CreateTestPrompt("code_review", metadata: new Dictionary<string, object?>());
        registry.Register(prompt);

        var result = await registry.ListPromptsAsync();

        var listed = result.Prompts[0];
        Assert.Null(listed.Meta);
    }

    [Fact]
    public async Task ListPromptsAsync_WithIcons_IncludesIcons()
    {
        var registry = new DefaultPromptRegistry();
        var icons = new List<Icon>
        {
            new() { Source = "https://example.com/icon.svg", MimeType = "image/svg+xml" }
        };
        var prompt = CreateTestPrompt("code_review", icons: icons);
        registry.Register(prompt);

        var result = await registry.ListPromptsAsync();

        var listed = result.Prompts[0];
        Assert.NotNull(listed.Icons);
        Assert.Single(listed.Icons);
        Assert.Equal("https://example.com/icon.svg", listed.Icons[0].Source);
        Assert.Equal("image/svg+xml", listed.Icons[0].MimeType);
    }

    [Fact]
    public async Task ListPromptsAsync_WithNullIcons_ExcludesIcons()
    {
        var registry = new DefaultPromptRegistry();
        var prompt = CreateTestPrompt("code_review", icons: null);
        registry.Register(prompt);

        var result = await registry.ListPromptsAsync();

        var listed = result.Prompts[0];
        Assert.Null(listed.Icons);
    }

    private static TestPrompt CreateTestPrompt(
        string name,
        string? title = null,
        string? description = null,
        IReadOnlyList<PromptArgument>? arguments = null,
        IList<Icon>? icons = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        return new TestPrompt
        {
            Name = name,
            Title = title,
            Description = description,
            Arguments = arguments,
            Icons = icons,
            Metadata = metadata ?? new Dictionary<string, object?>()
        };
    }

    private class TestPrompt : IMcpPrompt
    {
        public required string Name { get; init; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public IReadOnlyList<PromptArgument>? Arguments { get; set; }
        public IList<Icon>? Icons { get; set; }
        public IReadOnlyDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>();

        public Task<GetPromptResult> GetAsync(RequestContext<GetPromptRequestParams> getPromptRequest, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
