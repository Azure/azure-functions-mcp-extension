// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace ConformanceTestApp.Resources;

/// <summary>
/// Resources required by the MCP conformance "active" server suite.
/// URIs and payload shapes mirror the scenarios in
/// https://github.com/modelcontextprotocol/conformance.
/// </summary>
public class ConformanceResources
{
    /// <summary>Scenario: resources-read-text.</summary>
    [Function("static_text_resource")]
    public string StaticText(
        [McpResourceTrigger(
            "test://static-text",
            "static-text",
            Description = "Static text resource for conformance tests.",
            MimeType = "text/plain")]
        ResourceInvocationContext context)
        => ConformanceFixtures.StaticTextContent;

    /// <summary>Scenario: resources-read-binary.</summary>
    [Function("static_binary_resource")]
    public byte[] StaticBinary(
        [McpResourceTrigger(
            "test://static-binary",
            "static-binary",
            Description = "Static binary resource for conformance tests.",
            MimeType = "image/png")]
        ResourceInvocationContext context)
        => ConformanceFixtures.OnePixelPng;

    /// <summary>
    /// Scenario: resources-templates-read.
    /// Template URI is <c>test://template/{id}/data</c>; the suite asks for
    /// <c>test://template/123/data</c> and asserts the substituted id
    /// appears in the response body.
    /// </summary>
    [Function("template_data_resource")]
    public string TemplateData(
        [McpResourceTrigger(
            "test://template/{id}/data",
            "template-data",
            Description = "Templated resource that echoes the id parameter.",
            MimeType = "application/json")]
        ResourceInvocationContext context,
        string id)
        => $$"""{"id":"{{id}}","templateTest":true,"data":"Data for ID: {{id}}"}""";
}
