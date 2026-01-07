// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace Worker.Extensions.Mcp.Sdk.Tests;

public class McpJsonContextTests
{
    [Fact]
    public void McpToolResult_SerializesAndDeserializes_Successfully()
    {
        // Arrange
        var original = new McpToolResult
        {
            Type = "text",
            Content = "{\"text\":\"Hello World\"}"
        };

        // Act
        var json = JsonSerializer.Serialize(original, McpJsonContext.Default.McpToolResult);
        var deserialized = JsonSerializer.Deserialize<McpToolResult>(json, McpJsonContext.Default.McpToolResult);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Type, deserialized.Type);
        Assert.Equal(original.Content, deserialized.Content);
    }

    [Fact]
    public void McpToolResult_WithNullContent_SerializesCorrectly()
    {
        // Arrange
        var original = new McpToolResult
        {
            Type = "empty",
            Content = null
        };

        // Act
        var json = JsonSerializer.Serialize(original, McpJsonContext.Default.McpToolResult);
        var deserialized = JsonSerializer.Deserialize<McpToolResult>(json, McpJsonContext.Default.McpToolResult);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Type, deserialized.Type);
        Assert.Null(deserialized.Content);
    }

    [Fact]
    public void McpToolResult_PropertyNames_AreLowercase()
    {
        // Arrange
        var toolResult = new McpToolResult
        {
            Type = "text",
            Content = "content"
        };

        // Act
        var json = JsonSerializer.Serialize(toolResult, McpJsonContext.Default.McpToolResult);

        // Assert
        Assert.Contains("\"type\":", json);
        Assert.Contains("\"content\":", json);
        Assert.DoesNotContain("\"Type\":", json);
        Assert.DoesNotContain("\"Content\":", json);
    }

    [Fact]
    public void McpToolResult_Deserialization_IsCaseInsensitive()
    {
        // Arrange
        var jsonWithUpperCase = "{\"Type\":\"text\",\"Content\":\"value\"}";
        var jsonWithLowerCase = "{\"type\":\"text\",\"content\":\"value\"}";

        // Act
        var fromUpperCase = JsonSerializer.Deserialize<McpToolResult>(jsonWithUpperCase, McpJsonContext.Default.McpToolResult);
        var fromLowerCase = JsonSerializer.Deserialize<McpToolResult>(jsonWithLowerCase, McpJsonContext.Default.McpToolResult);

        // Assert
        Assert.NotNull(fromUpperCase);
        Assert.NotNull(fromLowerCase);
        Assert.Equal(fromUpperCase.Type, fromLowerCase.Type);
        Assert.Equal(fromUpperCase.Content, fromLowerCase.Content);
    }

    [Fact]
    public void McpToolResult_WithLargeContent_SerializesSuccessfully()
    {
        // Arrange
        var largeContent = new string('x', 10000);
        var toolResult = new McpToolResult
        {
            Type = "text",
            Content = largeContent
        };

        // Act
        var json = JsonSerializer.Serialize(toolResult, McpJsonContext.Default.McpToolResult);
        var deserialized = JsonSerializer.Deserialize<McpToolResult>(json, McpJsonContext.Default.McpToolResult);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(largeContent, deserialized.Content);
    }

    [Fact]
    public void McpToolResult_WithSpecialCharacters_SerializesCorrectly()
    {
        // Arrange
        var specialContent = "Line1\nLine2\tTab\"Quote\"\\Backslash";
        var toolResult = new McpToolResult
        {
            Type = "text",
            Content = specialContent
        };

        // Act
        var json = JsonSerializer.Serialize(toolResult, McpJsonContext.Default.McpToolResult);
        var deserialized = JsonSerializer.Deserialize<McpToolResult>(json, McpJsonContext.Default.McpToolResult);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(specialContent, deserialized.Content);
    }
}
