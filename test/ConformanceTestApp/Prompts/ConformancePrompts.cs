// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using ModelContextProtocol.Protocol;

namespace ConformanceTestApp.Prompts;

/// <summary>
/// Prompts required by the MCP conformance "active" server suite.
/// Names and message shapes mirror the scenarios in
/// https://github.com/modelcontextprotocol/conformance.
/// </summary>
public class ConformancePrompts
{
    /// <summary>Scenario: prompts-get-simple.</summary>
    [Function("test_simple_prompt")]
    public string SimplePrompt(
        [McpPromptTrigger(
            "test_simple_prompt",
            Description = "Returns a single user message with simple text content.")]
        PromptInvocationContext context)
        => ConformanceFixtures.SimplePromptText;

    /// <summary>
    /// Scenario: prompts-get-with-args. The suite invokes this with
    /// arbitrary string values and asserts both appear verbatim in the
    /// returned message text.
    /// </summary>
    [Function("test_prompt_with_arguments")]
    public string PromptWithArguments(
        [McpPromptTrigger(
            "test_prompt_with_arguments",
            Description = "Echoes its two arguments back in the prompt body.")]
        PromptInvocationContext context,
        [McpPromptArgument("arg1", "First test argument.", isRequired: true)] string? arg1,
        [McpPromptArgument("arg2", "Second test argument.", isRequired: true)] string? arg2)
        => $"Prompt with arguments: arg1='{arg1}', arg2='{arg2}'";

    /// <summary>Scenario: prompts-get-embedded-resource.</summary>
    [Function("test_prompt_with_embedded_resource")]
    public GetPromptResult PromptWithEmbeddedResource(
        [McpPromptTrigger(
            "test_prompt_with_embedded_resource",
            Description = "Returns a prompt containing an embedded resource and a follow-up text message.")]
        PromptInvocationContext context,
        [McpPromptArgument("resourceUri", "URI to embed in the prompt.", isRequired: true)] string? resourceUri)
        => new()
        {
            Messages =
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new EmbeddedResourceBlock
                    {
                        Resource = new TextResourceContents
                        {
                            Uri = resourceUri ?? "test://example-resource",
                            MimeType = "text/plain",
                            Text = ConformanceFixtures.EmbeddedResourceText
                        }
                    }
                },
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock
                    {
                        Text = "Please process the embedded resource above."
                    }
                }
            ]
        };

    /// <summary>Scenario: prompts-get-with-image.</summary>
    [Function("test_prompt_with_image")]
    public GetPromptResult PromptWithImage(
        [McpPromptTrigger(
            "test_prompt_with_image",
            Description = "Returns a prompt containing an image and a follow-up text message.")]
        PromptInvocationContext context)
        => new()
        {
            Messages =
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = ImageContentBlock.FromBytes(ConformanceFixtures.OnePixelPng, "image/png")
                },
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock
                    {
                        Text = "Please analyze the image above."
                    }
                }
            ]
        };
}
