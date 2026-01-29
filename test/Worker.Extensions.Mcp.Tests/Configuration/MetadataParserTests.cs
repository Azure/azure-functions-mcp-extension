// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests.Configuration;

public class MetadataParserTests
{
    [Fact]
    public void TryExtractMetadataFromParameter_NoTriggerAttribute_ReturnsFalse()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.NoTrigger))!;
        var parameters = method.GetParameters();

        var result = MetadataParser.TryExtractMetadataFromParameter<McpResourceTriggerAttribute>(
            parameters, out var metadataJson);

        Assert.False(result);
        Assert.Null(metadataJson);
    }

    [Fact]
    public void TryExtractMetadataFromParameter_TriggerWithoutMetadata_ReturnsFalse()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.TriggerNoMetadata))!;
        var parameters = method.GetParameters();

        var result = MetadataParser.TryExtractMetadataFromParameter<McpResourceTriggerAttribute>(
            parameters, out var metadataJson);

        Assert.False(result);
        Assert.Null(metadataJson);
    }

    [Fact]
    public void TryExtractMetadataFromParameter_TriggerWithMetadata_ReturnsJsonString()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.TriggerWithMetadata))!;
        var parameters = method.GetParameters();

        var result = MetadataParser.TryExtractMetadataFromParameter<McpResourceTriggerAttribute>(
            parameters, out var metadataJson);

        Assert.True(result);
        Assert.NotNull(metadataJson);

        var json = JsonNode.Parse(metadataJson)!.AsObject();
        Assert.Equal("John", json["author"]!.GetValue<string>());
        Assert.Equal("1.0", json["version"]!.GetValue<string>());
    }

    [Fact]
    public void TryExtractMetadataFromParameter_TriggerWithNestedMetadata_ReturnsNestedJson()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.TriggerWithNestedMetadata))!;
        var parameters = method.GetParameters();

        var result = MetadataParser.TryExtractMetadataFromParameter<McpResourceTriggerAttribute>(
            parameters, out var metadataJson);

        Assert.True(result);
        Assert.NotNull(metadataJson);

        var json = JsonNode.Parse(metadataJson)!.AsObject();
        var ui = json["ui"]!.AsObject();
        Assert.Equal("ui://test/widget", ui["resourceUri"]!.GetValue<string>());
        Assert.True(ui["prefersBorder"]!.GetValue<bool>());
    }

    [Fact]
    public void TryExtractMetadataFromParameter_InvalidJson_ReturnsFalse()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.TriggerWithInvalidJson))!;
        var parameters = method.GetParameters();

        var result = MetadataParser.TryExtractMetadataFromParameter<McpResourceTriggerAttribute>(
            parameters, out var metadataJson);

        Assert.False(result);
        Assert.Null(metadataJson);
    }

    [Fact]
    public void TryExtractMetadataFromParameter_EmptyJson_ReturnsFalse()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.TriggerWithEmptyJson))!;
        var parameters = method.GetParameters();

        var result = MetadataParser.TryExtractMetadataFromParameter<McpResourceTriggerAttribute>(
            parameters, out var metadataJson);

        Assert.False(result);
        Assert.Null(metadataJson);
    }

    [Fact]
    public void TryExtractMetadataFromParameter_ToolTrigger_ReturnsMetadata()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.ToolTriggerWithMetadata))!;
        var parameters = method.GetParameters();

        var result = MetadataParser.TryExtractMetadataFromParameter<McpToolTriggerAttribute>(
            parameters, out var metadataJson);

        Assert.True(result);
        Assert.NotNull(metadataJson);

        var json = JsonNode.Parse(metadataJson)!.AsObject();
        Assert.Equal("utility", json["category"]!.GetValue<string>());
    }

    private class TestClass
    {
        public void NoTrigger(string value) { }

        public void TriggerNoMetadata(
            [McpResourceTrigger("file://test", "test")] ResourceInvocationContext context) { }

        public void TriggerWithMetadata(
            [McpResourceTrigger("file://test", "test")]
            [McpMetadata("{\"author\":\"John\",\"version\":\"1.0\"}")] ResourceInvocationContext context) { }

        public void TriggerWithNestedMetadata(
            [McpResourceTrigger("file://test", "test")]
            [McpMetadata("{\"ui\":{\"resourceUri\":\"ui://test/widget\",\"prefersBorder\":true}}")] ResourceInvocationContext context) { }

        public void TriggerWithInvalidJson(
            [McpResourceTrigger("file://test", "test")]
            [McpMetadata("not valid json")] ResourceInvocationContext context) { }

        public void TriggerWithEmptyJson(
            [McpResourceTrigger("file://test", "test")]
            [McpMetadata("")] ResourceInvocationContext context) { }

        public void ToolTriggerWithMetadata(
            [McpToolTrigger("TestTool")]
            [McpMetadata("{\"category\":\"utility\"}")] ToolInvocationContext context) { }
    }

    #region Tool Trigger Metadata Tests

    [Fact]
    public void TryExtractMetadataFromParameter_ToolTriggerNoMetadata_ReturnsFalse()
    {
        var method = typeof(ToolTestClass).GetMethod(nameof(ToolTestClass.ToolNoMetadata))!;
        var parameters = method.GetParameters();

        var result = MetadataParser.TryExtractMetadataFromParameter<McpToolTriggerAttribute>(
            parameters, out var metadataJson);

        Assert.False(result);
        Assert.Null(metadataJson);
    }

    [Fact]
    public void TryExtractMetadataFromParameter_ToolTriggerWithMetadata_ReturnsMetadata()
    {
        var method = typeof(ToolTestClass).GetMethod(nameof(ToolTestClass.ToolWithMetadata))!;
        var parameters = method.GetParameters();

        var result = MetadataParser.TryExtractMetadataFromParameter<McpToolTriggerAttribute>(
            parameters, out var metadataJson);

        Assert.True(result);
        Assert.NotNull(metadataJson);

        var json = JsonNode.Parse(metadataJson)!.AsObject();
        Assert.Equal(1.0, json["version"]!.GetValue<double>());
        Assert.Equal("Jane", json["author"]!.GetValue<string>());
    }

    private class ToolTestClass
    {
        public void ToolNoMetadata(
            [McpToolTrigger("test-tool", "A test tool")] ToolInvocationContext context) { }

        public void ToolWithMetadata(
            [McpToolTrigger("test-tool", "A test tool")]
            [McpMetadata("""{"version": 1.0, "author": "Jane"}""")] ToolInvocationContext context) { }
    }

    #endregion
}
