// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated.Prompts;

/// <summary>
/// Prompt functions that test the McpPromptTrigger feature set:
/// argument binding, required/optional args, metadata, and edge cases.
/// </summary>
public class PromptFunctions(ILogger<PromptFunctions> logger)
{
    /// <summary>
    /// A prompt with multiple arguments (1 required, 1 optional).
    /// Tests multi-argument prompt with mixed required/optional + Title + Description.
    /// </summary>
    [Function(nameof(CodeReviewPrompt))]
    public string CodeReviewPrompt(
        [McpPromptTrigger(
            "code_review",
            Title = "Code Review",
            Description = "Generates a code review prompt for the given code snippet")]
        PromptInvocationContext context,
        [McpPromptArgument("code", "The code to review", isRequired: true)] string? code,
        [McpPromptArgument("language", "The programming language")] string? language)
    {
        logger.LogInformation("Generating code review prompt");

        code ??= "// no code provided";
        language ??= "unknown";

        return $"Please review the following {language} code and suggest improvements:\n\n```{language}\n{code}\n```";
    }

    /// <summary>
    /// A prompt with a single required argument and string return.
    /// Tests auto-wrapping of plain string into PromptMessage.
    /// </summary>
    [Function(nameof(SummarizePrompt))]
    public string SummarizePrompt(
        [McpPromptTrigger(
            "summarize",
            Title = "Summarize Text",
            Description = "Summarizes the provided text")]
        PromptInvocationContext context,
        [McpPromptArgument("text", "The text to summarize", isRequired: true)] string? text)
    {
        logger.LogInformation("Generating summarize prompt");

        text ??= "No text provided";
        return $"Please provide a concise summary of the following text:\n\n{text}";
    }

    /// <summary>
    /// A prompt with no arguments at all. Tests the edge case of a prompt
    /// that takes no user input.
    /// </summary>
    [Function(nameof(NoArgsPrompt))]
    public string NoArgsPrompt(
        [McpPromptTrigger(
            "no_args_prompt",
            Title = "No Arguments Prompt",
            Description = "A prompt that requires no arguments")]
        PromptInvocationContext context)
    {
        logger.LogInformation("Generating no-args prompt");
        return "This prompt requires no arguments. Please provide general guidance.";
    }

    /// <summary>
    /// A prompt where all arguments are optional (have defaults).
    /// Tests invocation with no arguments, partial arguments, and all arguments.
    /// </summary>
    [Function(nameof(AllOptionalArgsPrompt))]
    public string AllOptionalArgsPrompt(
        [McpPromptTrigger(
            "all_optional_args_prompt",
            Title = "All Optional Arguments",
            Description = "A prompt where every argument is optional")]
        PromptInvocationContext context,
        [McpPromptArgument("topic", "The topic to discuss")] string? topic,
        [McpPromptArgument("style", "The writing style")] string? style)
    {
        logger.LogInformation("Generating all-optional-args prompt");

        topic ??= "general";
        style ??= "concise";

        return $"Please write about {topic} in a {style} style.";
    }

    /// <summary>
    /// A prompt with [McpMetadata] attribute. Tests metadata on prompts.
    /// Also has additional metadata configured via fluent API in Program.cs.
    /// </summary>
    [Function(nameof(MetadataPrompt))]
    public string MetadataPrompt(
        [McpPromptTrigger(
            "metadata_prompt",
            Title = "Metadata Prompt",
            Description = "A prompt with metadata attached")]
        [McpMetadata("""{"category": "testing", "priority": "high"}""")]
        PromptInvocationContext context,
        [McpPromptArgument("input", "Input text", isRequired: true)] string? input)
    {
        logger.LogInformation("Generating metadata prompt");
        return $"Process the following with metadata context: {input ?? "(empty)"}";
    }

    /// <summary>
    /// A prompt defined with arguments via the fluent builder API in Program.cs.
    /// Tests ConfigureMcpPrompt().WithArgument().WithMetadata().
    /// </summary>
    [Function(nameof(FluentPrompt))]
    public string FluentPrompt(
        [McpPromptTrigger(
            "fluent_prompt",
            Title = "Fluent Prompt",
            Description = "A prompt with arguments defined via the fluent builder API")]
        PromptInvocationContext context)
    {
        logger.LogInformation("Generating fluent prompt");
        // Arguments are defined via builder in Program.cs, accessed through the context
        return "This prompt has its arguments defined via the fluent builder API.";
    }
}
