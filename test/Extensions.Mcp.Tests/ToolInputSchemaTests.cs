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
    public void PropertyBasedToolInputSchema_GetSchemaElement_GeneratesCorrectSchema()
    {
        // Arrange
        var properties = new[]
        {
            new TestMcpToolProperty 
            { 
                PropertyName = "name", 
                PropertyType = "string", 
                Description = "The name", 
                IsRequired = true 
            },
            new TestMcpToolProperty 
            { 
                PropertyName = "age", 
                PropertyType = "integer", 
                Description = "The age", 
                IsRequired = false 
            }
        };
        var schema = new PropertyBasedToolInputSchema(properties);

        // Act
        var element = schema.GetSchemaElement();

        // Assert
        Assert.Equal("object", element.GetProperty("type").GetString());
        
        var props = element.GetProperty("properties");
        Assert.True(props.TryGetProperty("name", out var nameProperty));
        Assert.Equal("string", nameProperty.GetProperty("type").GetString());
        Assert.Equal("The name", nameProperty.GetProperty("description").GetString());
        
        Assert.True(props.TryGetProperty("age", out var ageProperty));
        Assert.Equal("integer", ageProperty.GetProperty("type").GetString());
        Assert.Equal("The age", ageProperty.GetProperty("description").GetString());
        
        var required = element.GetProperty("required");
        Assert.Single(required.EnumerateArray());
        Assert.Equal("name", required[0].GetString());
    }

    [Fact]
    public void JsonSchemaToolInputSchema_GetSchemaElement_ReturnsOriginalSchema()
    {
        // Arrange
        var schemaJson = """
            {
                "type": "object",
                "properties": {
                    "testProp": {
                        "type": "string",
                        "description": "A test property"
                    }
                },
                "required": ["testProp"]
            }
            """;
        var jsonDoc = CreateDocumentFromJson(schemaJson);
        var schema = new JsonSchemaToolInputSchema(jsonDoc);

        // Act
        var element = schema.GetSchemaElement();

        // Assert
        Assert.Equal("object", element.GetProperty("type").GetString());
        
        var props = element.GetProperty("properties");
        Assert.True(props.TryGetProperty("testProp", out var testProperty));
        Assert.Equal("string", testProperty.GetProperty("type").GetString());
        Assert.Equal("A test property", testProperty.GetProperty("description").GetString());
        
        var required = element.GetProperty("required");
        Assert.Single(required.EnumerateArray());
        Assert.Equal("testProp", required[0].GetString());
    }

    [Fact]
    public void PropertyBasedToolInputSchema_WithEnumValues_GeneratesEnumConstraints()
    {
        // Arrange
        var properties = new[]
        {
            new TestMcpToolProperty 
            { 
                PropertyName = "status", 
                PropertyType = "string", 
                Description = "The status", 
                IsRequired = true,
                EnumValues = new[] { "active", "inactive", "pending" }
            }
        };
        var schema = new PropertyBasedToolInputSchema(properties);

        // Act
        var element = schema.GetSchemaElement();

        // Assert
        var props = element.GetProperty("properties");
        var statusProperty = props.GetProperty("status");
        Assert.Equal("string", statusProperty.GetProperty("type").GetString());
        Assert.True(statusProperty.TryGetProperty("enum", out var enumProperty));
        
        var enumValues = enumProperty.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(new[] { "active", "inactive", "pending" }, enumValues);
    }

    [Fact]
    public void PropertyBasedToolInputSchema_WithArrayProperty_GeneratesArraySchema()
    {
        // Arrange
        var properties = new[]
        {
            new TestMcpToolProperty 
            { 
                PropertyName = "tags", 
                PropertyType = "string", 
                Description = "List of tags", 
                IsRequired = false,
                IsArray = true,
                EnumValues = new[] { "important", "urgent", "normal" }
            }
        };
        var schema = new PropertyBasedToolInputSchema(properties);

        // Act
        var element = schema.GetSchemaElement();

        // Assert
        var props = element.GetProperty("properties");
        var tagsProperty = props.GetProperty("tags");
        Assert.Equal("array", tagsProperty.GetProperty("type").GetString());
        Assert.Equal("List of tags", tagsProperty.GetProperty("description").GetString());
        
        var items = tagsProperty.GetProperty("items");
        Assert.Equal("string", items.GetProperty("type").GetString());
        Assert.True(items.TryGetProperty("enum", out var enumProperty));
        
        var enumValues = enumProperty.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(new[] { "important", "urgent", "normal" }, enumValues);
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
    public void JsonSchemaValidator_ThrowsArgumentNullException_ForNullInputSchema()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new JsonSchemaToolInputSchema(null!));
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

    [Fact]
    public void ToolInputSchemas_BothTypesWorkWithMcpToolExtensions()
    {
        // This test verifies that both types of schemas work with the unified GetPropertiesInputSchema extension method

        // Test with PropertyBasedToolInputSchema
        var properties = new[]
        {
            new TestMcpToolProperty 
            { 
                PropertyName = "name", 
                PropertyType = "string", 
                IsRequired = true 
            }
        };
        var propertySchema = new PropertyBasedToolInputSchema(properties);
        var propertyElement = propertySchema.GetSchemaElement();
        
        // Test with JsonSchemaToolInputSchema  
        var schemaJson = """
            {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string"
                    }
                },
                "required": ["name"]
            }
            """;
        var jsonDoc = CreateDocumentFromJson(schemaJson);
        var jsonSchema = new JsonSchemaToolInputSchema(jsonDoc);
        var jsonElement = jsonSchema.GetSchemaElement();
        
        // Both should produce similar results
        Assert.Equal("object", propertyElement.GetProperty("type").GetString());
        Assert.Equal("object", jsonElement.GetProperty("type").GetString());
        
        Assert.True(propertyElement.TryGetProperty("properties", out var propertyProps));
        Assert.True(jsonElement.TryGetProperty("properties", out var jsonProps));
        
        Assert.True(propertyProps.TryGetProperty("name", out var _));
        Assert.True(jsonProps.TryGetProperty("name", out var _));
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
