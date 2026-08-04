// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using ModelContextProtocol.Protocol;

namespace ConformanceTestApp.Tools;

/// <summary>
/// Tools required by the MCP conformance "active" server suite.
///
/// Each function name and return shape is dictated by the corresponding
/// scenario in https://github.com/modelcontextprotocol/conformance. Do
/// not rename or change return shapes without updating the suite version
/// at the same time.
/// </summary>
public class ConformanceTools
{
    /// <summary>Scenario: tools-call-simple-text.</summary>
    [Function("test_simple_text")]
    public string SimpleText(
        [McpToolTrigger("test_simple_text", "Returns simple text content.")] ToolInvocationContext context)
        => ConformanceFixtures.SimpleTextToolResponse;

    /// <summary>Scenario: tools-call-image.</summary>
    [Function("test_image_content")]
    public ImageContentBlock Image(
        [McpToolTrigger("test_image_content", "Returns a single image content block.")] ToolInvocationContext context)
        => ImageContentBlock.FromBytes(ConformanceFixtures.OnePixelPng, "image/png");

    /// <summary>Scenario: tools-call-audio.</summary>
    [Function("test_audio_content")]
    public AudioContentBlock Audio(
        [McpToolTrigger("test_audio_content", "Returns a single audio content block.")] ToolInvocationContext context)
        => AudioContentBlock.FromBytes(ConformanceFixtures.EmptyWav, "audio/wav");

    /// <summary>Scenario: tools-call-embedded-resource.</summary>
    [Function("test_embedded_resource")]
    public EmbeddedResourceBlock EmbeddedResource(
        [McpToolTrigger("test_embedded_resource", "Returns an embedded resource content block.")] ToolInvocationContext context)
        => new()
        {
            Resource = new TextResourceContents
            {
                Uri = "test://embedded-resource",
                MimeType = "text/plain",
                Text = ConformanceFixtures.EmbeddedResourceText
            }
        };

    /// <summary>Scenario: tools-call-mixed-content.</summary>
    [Function("test_multiple_content_types")]
    public IList<ContentBlock> MixedContent(
        [McpToolTrigger("test_multiple_content_types", "Returns text, image, and embedded-resource content blocks.")] ToolInvocationContext context)
        =>
        [
            new TextContentBlock { Text = "Multiple content types test:" },
            ImageContentBlock.FromBytes(ConformanceFixtures.OnePixelPng, "image/png"),
            new EmbeddedResourceBlock
            {
                Resource = new TextResourceContents
                {
                    Uri = "test://mixed-content-resource",
                    MimeType = "application/json",
                    Text = """{"test":"data","value":123}"""
                }
            }
        ];

    /// <summary>
    /// Scenario: tools-call-error. Returns an MCP tool error (isError=true)
    /// without throwing, per the spec's "tool execution failure" pattern.
    /// </summary>
    [Function("test_error_handling")]
    public CallToolResult Error(
        [McpToolTrigger("test_error_handling", "Always returns an isError result for conformance testing.")] ToolInvocationContext context)
        => new()
        {
            IsError = true,
            Content =
            [
                new TextContentBlock { Text = ConformanceFixtures.ErrorToolMessage }
            ]
        };
}
