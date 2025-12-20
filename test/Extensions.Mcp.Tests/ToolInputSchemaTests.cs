// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Validation;
using ModelContextProtocol.Protocol;
using ModelContextProtocol;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class ToolInputSchemaTests
{
    private static JsonDocument CreateDocumentFromJson(string json)
    {
        return JsonDocument.Parse(json);
    }

    private static CallToolRequestParams CreateRequest(params (string key, JsonElement value)[] args)
    {
        var dict = args?.ToDictionary(x => x.key, x => x.value) ?? new Dictionary<string, JsonElement>();
        return new CallToolRequestParams { Name = "testTool", Arguments = dict };
    }

    [Fact]
    public void PropertyBasedValidator_ThrowsException_WhenRequiredPropertyMissing()
    {
        // Arrange
        var properties = new[]
        {
            new TestMcpToolProperty { PropertyName = "requiredProp", IsRequired = true }
        };
        var validator = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequest(); // No arguments

        // Act & Assert
        var ex = Assert.Throws<McpProtocolException>(() => validator.Validate(request));
        Assert.Contains("requiredProp", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyPresent()
    {
        // Arrange
        var properties = new[]
        {
            new TestMcpToolProperty { PropertyName = "requiredProp", IsRequired = true }
        };
        var validator = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequest(("requiredProp", JsonDocument.Parse("\"value\"").RootElement));

        // Act & Assert
        var ex = Record.Exception(() => validator.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenNoRequiredProperties()
    {
        // Arrange
        var properties = new[]
        {
            new TestMcpToolProperty { PropertyName = "optionalProp", IsRequired = false }
        };
        var validator = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequest(); // No arguments

        // Act & Assert
        var ex = Record.Exception(() => validator.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void JsonSchemaValidator_ThrowsException_WhenRequiredPropertyMissing()
    {
        // Arrange
        var schemaJson = """
            {
                "type": "object",
                "required": ["requiredFromSchema"]
            }
            """;
        var schema = CreateDocumentFromJson(schemaJson);
        var validator = new JsonSchemaToolInputSchema(schema);
        var request = CreateRequest(); // No arguments

        // Act & Assert
        var ex = Assert.Throws<McpProtocolException>(() => validator.Validate(request));
        Assert.Contains("requiredFromSchema", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public void JsonSchemaValidator_DoesNotThrow_WhenRequiredPropertyPresent()
    {
        // Arrange
        var schemaJson = """
            {
                "type": "object",
                "required": ["requiredFromSchema"]
            }
            """;
        var schema = CreateDocumentFromJson(schemaJson);
        var validator = new JsonSchemaToolInputSchema(schema);
        var request = CreateRequest(("requiredFromSchema", JsonDocument.Parse("\"value\"").RootElement));

        // Act & Assert
        var ex = Record.Exception(() => validator.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void JsonSchemaValidator_DoesNotThrow_WhenNoRequiredProperties()
    {
        // Arrange
        var schemaJson = """
            {
                "type": "object",
                "required": []
            }
            """;
        var schema = CreateDocumentFromJson(schemaJson);
        var validator = new JsonSchemaToolInputSchema(schema);
        var request = CreateRequest(); // No arguments

        // Act & Assert
        var ex = Record.Exception(() => validator.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void PropertyBasedValidator_ThrowsException_ForNullProperty()
    {
        // Arrange
        var properties = new[]
        {
            new TestMcpToolProperty { PropertyName = "requiredProp", IsRequired = true }
        };
        var validator = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequest(("requiredProp", JsonDocument.Parse("null").RootElement));

        // Act & Assert
        var ex = Assert.Throws<McpProtocolException>(() => validator.Validate(request));
        Assert.Contains("requiredProp", ex.Message);
    }

    [Fact]
    public void JsonSchemaValidator_ThrowsException_ForNullProperty()
    {
        // Arrange
        var schemaJson = """
            {
                "type": "object",
                "required": ["requiredFromSchema"]
            }
            """;
        var schema = CreateDocumentFromJson(schemaJson);
        var validator = new JsonSchemaToolInputSchema(schema);
        var request = CreateRequest(("requiredFromSchema", JsonDocument.Parse("null").RootElement));

        // Act & Assert
        var ex = Assert.Throws<McpProtocolException>(() => validator.Validate(request));
        Assert.Contains("requiredFromSchema", ex.Message);
    }

    [Fact]
    public void PropertyBasedValidator_ThrowsArgumentNullException_ForNullProperties()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PropertyBasedToolInputSchema(null!));
    }

    [Fact]
    public void PropertyBasedValidator_HandlesMultipleMissingProperties()
    {
        // Arrange
        var properties = new[]
        {
            new TestMcpToolProperty { PropertyName = "prop1", IsRequired = true },
            new TestMcpToolProperty { PropertyName = "prop2", IsRequired = true }
        };
        var validator = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequest(); // No arguments

        // Act & Assert
        var ex = Assert.Throws<McpProtocolException>(() => validator.Validate(request));
        Assert.Contains("prop1", ex.Message);
        Assert.Contains("prop2", ex.Message);
    }

    private class TestMcpToolProperty : IMcpToolProperty
    {
        public string PropertyName { get; set; } = string.Empty;
        public string PropertyType { get; set; } = "string";
        public string? Description { get; set; }
        public bool IsRequired { get; set; }
        public bool IsArray { get; set; }
        public IReadOnlyList<string> EnumValues { get; set; } = [];
    }
}
