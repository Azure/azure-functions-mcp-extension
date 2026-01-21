// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Xunit;

namespace Worker.Extensions.Mcp.Tests;

public class InputSchemaBindingPatcherTests
{
    [Fact]
    public void PatchBindingMetadata_EmptyBindingProperties_DoesNothing()
    {
        // Arrange
        var inputSchema = CreateValidSchema();
        var emptyBindings = Array.Empty<ToolPropertyBinding>();

        // Act & Assert - Should not throw
        InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, emptyBindings);
    }

    [Fact]
    public void PatchBindingMetadata_NullInputSchema_ThrowsArgumentNullException()
    {
        // Arrange
        var bindings = new[] { CreateToolPropertyBinding("test") };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            InputSchemaBindingPatcher.PatchBindingMetadata(null!, bindings));
    }

    [Fact]
    public void PatchBindingMetadata_NullBindingProperties_ThrowsArgumentNullException()
    {
        // Arrange
        var inputSchema = CreateValidSchema();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, null!));
    }

    [Fact]
    public void PatchBindingMetadata_ValidStringProperty_PatchesSuccessfully()
    {
        // Arrange
        var inputSchema = CreateSchemaWithStringProperty("name");
        var binding = CreateToolPropertyBinding("name");

        // Act
        InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, new[] { binding });

        // Assert
        Assert.Equal("string", binding.Binding[Constants.McpToolPropertyType]?.GetValue<string>());
    }

    [Fact]
    public void PatchBindingMetadata_ValidIntegerProperty_PatchesSuccessfully()
    {
        // Arrange
        var inputSchema = CreateSchemaWithIntegerProperty("age");
        var binding = CreateToolPropertyBinding("age");

        // Act
        InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, new[] { binding });

        // Assert
        Assert.Equal("integer", binding.Binding[Constants.McpToolPropertyType]?.GetValue<string>());
    }

    [Fact]
    public void PatchBindingMetadata_ValidArrayProperty_ExtractsItemType()
    {
        // Arrange
        var inputSchema = CreateSchemaWithArrayProperty("tags", "string");
        var binding = CreateToolPropertyBinding("tags");

        // Act
        InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, new[] { binding });

        // Assert
        Assert.Equal("string", binding.Binding[Constants.McpToolPropertyType]?.GetValue<string>());
    }

    [Fact]
    public void PatchBindingMetadata_PropertyNotInSchema_DoesNotPatch()
    {
        // Arrange
        var inputSchema = CreateSchemaWithStringProperty("name");
        var binding = CreateToolPropertyBinding("nonexistent");

        // Act
        InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, new[] { binding });

        // Assert - Property should not be patched
        Assert.False(binding.Binding.ContainsKey(Constants.McpToolPropertyType));
    }

    [Fact]
    public void PatchBindingMetadata_SchemaWithoutProperties_ThrowsInvalidOperationException()
    {
        // Arrange
        var inputSchema = JsonNode.Parse("""{"type": "object"}""")!;
        var binding = CreateToolPropertyBinding("test");

        // Act & Assert
        Assert.Throws<JsonException>(() => 
            InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, new[] { binding }));
    }

    [Fact]
    public void PatchBindingMetadata_InvalidPropertiesType_ThrowsInvalidOperationException()
    {
        // Arrange
        var inputSchema = JsonNode.Parse("""{"type": "object", "properties": "invalid"}""")!;
        var binding = CreateToolPropertyBinding("test");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, new[] { binding }));
    }

    [Fact]
    public void PatchBindingMetadata_PropertyWithoutType_DoesNotPatch()
    {
        // Arrange
        var inputSchema = JsonNode.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": {
                        "description": "Missing type property"
                    }
                }
            }
            """)!;
        var binding = CreateToolPropertyBinding("name");

        // Act
        InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, new[] { binding });

        // Assert - Property should not be patched
        Assert.False(binding.Binding.ContainsKey(Constants.McpToolPropertyType));
    }

    [Fact]
    public void PatchBindingMetadata_ArrayWithoutItems_DoesNotPatch()
    {
        // Arrange
        var inputSchema = JsonNode.Parse("""
            {
                "type": "object",
                "properties": {
                    "tags": {
                        "type": "array"
                    }
                }
            }
            """)!;
        var binding = CreateToolPropertyBinding("tags");

        // Act
        InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, new[] { binding });

        // Assert - Property should not be patched
        Assert.False(binding.Binding.ContainsKey(Constants.McpToolPropertyType));
    }

    [Fact]
    public void PatchBindingMetadata_ArrayItemsWithoutType_DoesNotPatch()
    {
        // Arrange
        var inputSchema = JsonNode.Parse("""
            {
                "type": "object",
                "properties": {
                    "tags": {
                        "type": "array",
                        "items": {
                            "description": "Missing type"
                        }
                    }
                }
            }
            """)!;
        var binding = CreateToolPropertyBinding("tags");

        // Act
        InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, new[] { binding });

        // Assert - Property should not be patched
        Assert.False(binding.Binding.ContainsKey(Constants.McpToolPropertyType));
    }

    [Fact]
    public void PatchBindingMetadata_MixedSuccessAndFailure_PatchesOnlyValidProperties()
    {
        // Arrange
        var inputSchema = JsonNode.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string"
                    },
                    "broken": {
                        "description": "Missing type property"
                    }
                }
            }
            """)!;
        
        var bindings = new[]
        {
            CreateToolPropertyBinding("name"),
            CreateToolPropertyBinding("broken"),
            CreateToolPropertyBinding("missing")
        };

        // Act
        InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, bindings);

        // Assert
        // Verify the successful one was patched
        Assert.Equal("string", bindings[0].Binding[Constants.McpToolPropertyType]?.GetValue<string>());
        
        // Verify the failed ones were not patched
        Assert.False(bindings[1].Binding.ContainsKey(Constants.McpToolPropertyType));
        Assert.False(bindings[2].Binding.ContainsKey(Constants.McpToolPropertyType));
    }

    [Fact]
    public void PatchBindingMetadata_ComplexNestedSchema_PatchesCorrectly()
    {
        // Arrange
        var inputSchema = JsonNode.Parse("""
            {
                "type": "object",
                "properties": {
                    "user": {
                        "type": "object",
                        "properties": {
                            "name": {
                                "type": "string"
                            },
                            "age": {
                                "type": "integer"
                            }
                        }
                    },
                    "tags": {
                        "type": "array",
                        "items": {
                            "type": "string"
                        }
                    },
                    "isActive": {
                        "type": "boolean"
                    }
                }
            }
            """)!;
        
        var bindings = new[]
        {
            CreateToolPropertyBinding("user"),
            CreateToolPropertyBinding("tags"),
            CreateToolPropertyBinding("isActive")
        };

        // Act
        InputSchemaBindingPatcher.PatchBindingMetadata(inputSchema, bindings);

        // Assert
        Assert.Equal("object", bindings[0].Binding[Constants.McpToolPropertyType]?.GetValue<string>());
        Assert.Equal("string", bindings[1].Binding[Constants.McpToolPropertyType]?.GetValue<string>()); // Array item type
        Assert.Equal("boolean", bindings[2].Binding[Constants.McpToolPropertyType]?.GetValue<string>());
    }

    private static JsonNode CreateValidSchema()
    {
        return JsonNode.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string"
                    }
                }
            }
            """)!;
    }

    private static JsonNode CreateSchemaWithStringProperty(string propertyName)
    {
        return JsonNode.Parse(@$"{{
                ""type"": ""object"",
                ""properties"": {{
                    ""{propertyName}"": {{
                        ""type"": ""string""
                    }}
                }}
            }}")!;
    }

    private static JsonNode CreateSchemaWithIntegerProperty(string propertyName)
    {
        return JsonNode.Parse(@$"{{
                ""type"": ""object"",
                ""properties"": {{
                    ""{propertyName}"": {{
                        ""type"": ""integer""
                    }}
                }}
            }}")!;
    }

    private static JsonNode CreateSchemaWithArrayProperty(string propertyName, string itemType)
    {
        return JsonNode.Parse(@$"{{
                ""type"": ""object"",
                ""properties"": {{
                    ""{propertyName}"": {{
                        ""type"": ""array"",
                        ""items"": {{
                            ""type"": ""{itemType}""
                        }}
                    }}
                }}
            }}")!;
    }

    private static ToolPropertyBinding CreateToolPropertyBinding(string propertyName)
    {
        var jsonObject = new JsonObject
        {
            ["type"] = Constants.McpToolPropertyBindingType,
            [Constants.McpToolPropertyName] = propertyName
        };
        
        return new ToolPropertyBinding(propertyName, jsonObject);
    }
}
