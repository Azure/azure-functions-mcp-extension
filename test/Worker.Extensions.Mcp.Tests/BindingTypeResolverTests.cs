// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Xunit;

namespace Worker.Extensions.Mcp.Tests;

public class BindingTypeResolverTests
{
    [Fact]
    public void ResolveAndApplyTypes_EmptyBindingProperties_DoesNothing()
    {
        // Arrange
        var inputSchema = CreateValidSchema();
        var emptyBindings = Array.Empty<KeyValuePair<string, ToolPropertyBinding>>();

        // Act & Assert - Should not throw
        BindingTypeResolver.ResolveAndApplyTypes(inputSchema, emptyBindings);
    }

    [Fact]
    public void ResolveAndApplyTypes_NullInputSchema_ThrowsArgumentNullException()
    {
        // Arrange
        var bindings = new[] { CreateToolPropertyBinding("test") };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            BindingTypeResolver.ResolveAndApplyTypes(null!, bindings));
    }

    [Fact]
    public void ResolveAndApplyTypes_NullBindingProperties_ThrowsArgumentNullException()
    {
        // Arrange
        var inputSchema = CreateValidSchema();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            BindingTypeResolver.ResolveAndApplyTypes(inputSchema, null!));
    }

    [Fact]
    public void ResolveAndApplyTypes_ValidStringProperty_PatchesSuccessfully()
    {
        // Arrange
        var inputSchema = CreateSchemaWithStringProperty("name");
        var binding = CreateToolPropertyBinding("name");

        // Act
        BindingTypeResolver.ResolveAndApplyTypes(inputSchema, new[] { binding });

        // Assert
        Assert.Equal("string", binding.Value.Binding[Constants.McpToolPropertyType]?.GetValue<string>());
    }

    [Fact]
    public void ResolveAndApplyTypes_ValidIntegerProperty_PatchesSuccessfully()
    {
        // Arrange
        var inputSchema = CreateSchemaWithIntegerProperty("age");
        var binding = CreateToolPropertyBinding("age");

            // Act
            BindingTypeResolver.ResolveAndApplyTypes(inputSchema, new[] { binding });

            // Assert
            Assert.Equal("integer", binding.Value.Binding[Constants.McpToolPropertyType]?.GetValue<string>());
        }

    [Fact]
    public void ResolveAndApplyTypes_ValidArrayProperty_ExtractsItemType()
    {
        // Arrange
        var inputSchema = CreateSchemaWithArrayProperty("tags", "string");
        var binding = CreateToolPropertyBinding("tags");

            // Act
            BindingTypeResolver.ResolveAndApplyTypes(inputSchema, new[] { binding });

            // Assert
            Assert.Equal("string", binding.Value.Binding[Constants.McpToolPropertyType]?.GetValue<string>());
        }

        [Fact]
        public void ResolveAndApplyTypes_PropertyNotInSchema_DoesNotPatch()
    {
        // Arrange
        var inputSchema = CreateSchemaWithStringProperty("name");
        var binding = CreateToolPropertyBinding("nonexistent");

            // Act
            BindingTypeResolver.ResolveAndApplyTypes(inputSchema, new[] { binding });

            // Assert - Property should not be patched
            Assert.False(binding.Value.Binding.ContainsKey(Constants.McpToolPropertyType));
        }

        [Fact]
        public void ResolveAndApplyTypes_SchemaWithoutProperties_ThrowsInvalidOperationException()
    {
        // Arrange
        var inputSchema = JsonNode.Parse("""{"type": "object"}""")!;
        var binding = CreateToolPropertyBinding("test");

        // Act & Assert
        Assert.Throws<JsonException>(() => 
            BindingTypeResolver.ResolveAndApplyTypes(inputSchema, new[] { binding }));
    }

    [Fact]
    public void ResolveAndApplyTypes_InvalidPropertiesType_ThrowsInvalidOperationException()
    {
        // Arrange
        var inputSchema = JsonNode.Parse("""{"type": "object", "properties": "invalid"}""")!;
        var binding = CreateToolPropertyBinding("test");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            BindingTypeResolver.ResolveAndApplyTypes(inputSchema, new[] { binding }));
    }

    [Fact]
    public void ResolveAndApplyTypes_PropertyWithoutType_DoesNotPatch()
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
            BindingTypeResolver.ResolveAndApplyTypes(inputSchema, new[] { binding });

            // Assert - Property should not be patched
            Assert.False(binding.Value.Binding.ContainsKey(Constants.McpToolPropertyType));
        }

        [Fact]
        public void ResolveAndApplyTypes_ArrayWithoutItems_DoesNotPatch()
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
            BindingTypeResolver.ResolveAndApplyTypes(inputSchema, new[] { binding });

            // Assert - Property should not be patched
            Assert.False(binding.Value.Binding.ContainsKey(Constants.McpToolPropertyType));
        }

        [Fact]
        public void ResolveAndApplyTypes_ArrayItemsWithoutType_DoesNotPatch()
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
            BindingTypeResolver.ResolveAndApplyTypes(inputSchema, new[] { binding });

            // Assert - Property should not be patched
            Assert.False(binding.Value.Binding.ContainsKey(Constants.McpToolPropertyType));
        }

        [Fact]
        public void ResolveAndApplyTypes_MixedSuccessAndFailure_PatchesOnlyValidProperties()
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
        BindingTypeResolver.ResolveAndApplyTypes(inputSchema, bindings);

            // Assert
            // Verify the successful one was patched
            Assert.Equal("string", bindings[0].Value.Binding[Constants.McpToolPropertyType]?.GetValue<string>());

            // Verify the failed ones were not patched
            Assert.False(bindings[1].Value.Binding.ContainsKey(Constants.McpToolPropertyType));
            Assert.False(bindings[2].Value.Binding.ContainsKey(Constants.McpToolPropertyType));
        }

    [Fact]
    public void ResolveAndApplyTypes_ComplexNestedSchema_PatchesCorrectly()
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
        BindingTypeResolver.ResolveAndApplyTypes(inputSchema, bindings);

            // Assert
            Assert.Equal("object", bindings[0].Value.Binding[Constants.McpToolPropertyType]?.GetValue<string>());
            Assert.Equal("string", bindings[1].Value.Binding[Constants.McpToolPropertyType]?.GetValue<string>()); // Array item type
            Assert.Equal("boolean", bindings[2].Value.Binding[Constants.McpToolPropertyType]?.GetValue<string>());
        }

    [Fact]
    public void ResolveAndApplyTypes_ArrayItemsAsString_DoesNotPatch()
    {
        // Arrange - items is a string instead of an object
        var inputSchema = JsonNode.Parse("""
            {
                "type": "object",
                "properties": {
                    "tags": {
                        "type": "array",
                        "items": "string"
                    }
                }
            }
            """)!;
        var binding = CreateToolPropertyBinding("tags");

        // Act
        BindingTypeResolver.ResolveAndApplyTypes(inputSchema, new[] { binding });

        // Assert - items is not an object, so the type should not be patched
        Assert.False(binding.Value.Binding.ContainsKey(Constants.McpToolPropertyType));
    }

    [Fact]
    public void ResolveAndApplyTypes_ArrayItemsAsArray_DoesNotPatch()
    {
        // Arrange - items is an array instead of an object
        var inputSchema = JsonNode.Parse("""
            {
                "type": "object",
                "properties": {
                    "tags": {
                        "type": "array",
                        "items": ["string", "integer"]
                    }
                }
            }
            """)!;
        var binding = CreateToolPropertyBinding("tags");

        // Act
        BindingTypeResolver.ResolveAndApplyTypes(inputSchema, new[] { binding });

        // Assert - items is not an object, so the type should not be patched
        Assert.False(binding.Value.Binding.ContainsKey(Constants.McpToolPropertyType));
    }

    [Fact]
    public void ResolveAndApplyTypes_ArrayItemsWithEmptyType_DoesNotPatch()
    {
        // Arrange - items has an empty type string
        var inputSchema = JsonNode.Parse("""
            {
                "type": "object",
                "properties": {
                    "tags": {
                        "type": "array",
                        "items": {
                            "type": ""
                        }
                    }
                }
            }
            """)!;
        var binding = CreateToolPropertyBinding("tags");

        // Act
        BindingTypeResolver.ResolveAndApplyTypes(inputSchema, new[] { binding });

        // Assert
        Assert.False(binding.Value.Binding.ContainsKey(Constants.McpToolPropertyType));
    }

    [Fact]
    public void ResolveAndApplyTypes_PropertySchemaNotObject_DoesNotPatch()
    {
        // Arrange - a property schema is a string value instead of an object
        var inputSchema = JsonNode.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": "string"
                }
            }
            """)!;
        var binding = CreateToolPropertyBinding("name");

        // Act
        BindingTypeResolver.ResolveAndApplyTypes(inputSchema, new[] { binding });

        // Assert
        Assert.False(binding.Value.Binding.ContainsKey(Constants.McpToolPropertyType));
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

            private static KeyValuePair<string, ToolPropertyBinding> CreateToolPropertyBinding(string propertyName, int index = 0)
            {
                var jsonObject = new JsonObject
                {
                    ["type"] = Constants.McpToolPropertyBindingType,
                    [Constants.McpToolPropertyName] = propertyName
                };

                return new KeyValuePair<string, ToolPropertyBinding>(propertyName, new ToolPropertyBinding(index, jsonObject));
            }
        }
