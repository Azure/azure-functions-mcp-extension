using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace TestAppIsolated;

public class PromptFunctions(ILogger<PromptFunctions> logger)
{
    /// <summary>
    /// A prompt that returns a GetPromptResult with structured messages.
    /// The SDK middleware serializes this into McpPromptResult format for the host.
    /// </summary>
    [Function(nameof(CodeReviewPrompt))]
    public GetPromptResult CodeReviewPrompt(
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

        return new GetPromptResult
        {
            Description = $"Code review prompt for {language}",
            Messages =
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock
                    {
                        Text = $"Please review the following {language} code and suggest improvements:\n\n```{language}\n{code}\n```"
                    }
                }
            ]
        };
    }

    /// <summary>
    /// A prompt that returns a plain string (tests the return value binder wrapping).
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
}
