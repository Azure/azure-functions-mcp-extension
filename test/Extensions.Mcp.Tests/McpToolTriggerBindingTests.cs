// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Serialization;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpToolTriggerBindingTests
{
    private static (McpToolTriggerBinding binding, ParameterInfo parameter) CreateBinding(ParameterInfo? parameter = null)
    {
        if (parameter is null)
        {
            var method = typeof(McpToolTriggerBindingTests).GetMethod(nameof(DummyMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
            parameter = method.GetParameters()[0];
        }

        var toolRegistry = new Mock<IToolRegistry>();
        var attribute = new McpToolTriggerAttribute("MyTool", "desc");

        var binding = new McpToolTriggerBinding(parameter, toolRegistry.Object, attribute);

        return (binding, parameter);
    }

    private static void DummyMethod([McpToolTrigger("MyTool", "desc")] ToolInvocationContext ctx) { }

    private static void DummyStringMethod([McpToolTrigger("MyTool", "desc")] string ctx) { }

    private static ValueBindingContext CreateValueBindingContext()
    {
        var functionContext = new FunctionBindingContext(Guid.NewGuid(), CancellationToken.None);
        return new ValueBindingContext(functionContext, CancellationToken.None);
    }

    [Fact]
    public async Task BindAsync_BasicBinding_PopulatesBindingData()
    {
        var (binding, param) = CreateBinding();
        CallToolExecutionContext executionContext = CallToolExecutionContextHelper.CreateExecutionContext();
        var triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        Assert.True(triggerData.BindingData.ContainsKey("mcptoolcontext"));
        Assert.True(triggerData.BindingData.ContainsKey(param.Name!));
        Assert.True(triggerData.BindingData.ContainsKey("mcptoolargs"));
        Assert.True(triggerData.BindingData.ContainsKey("mcpsessionid"));

        var ctx = Assert.IsType<ToolInvocationContext>(triggerData.BindingData["mcptoolcontext"]);
        Assert.Equal("MyTool", ctx.Name);
        Assert.Equal("session-123", ctx.SessionId);
    }

    [Fact]
    public async Task BindAsync_StringParameter_SerializesContext()
    {
        var method = typeof(McpToolTriggerBindingTests).GetMethod(nameof(DummyStringMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var param = method.GetParameters()[0];

        var (binding, _) = CreateBinding(param);
        CallToolExecutionContext executionContext = CallToolExecutionContextHelper.CreateExecutionContext();
        ITriggerData triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        var serialized = Assert.IsType<string>(triggerData.BindingData[param.Name!]);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<ToolInvocationContext>(serialized, McpJsonSerializerOptions.DefaultOptions);
        Assert.Equal("MyTool", deserialized!.Name);
    }

    [Fact]
    public async Task BindAsync_WithHttpContextAccessor_SetsHttpTransportAndHeaders()
    {
        var (binding, _) = CreateBinding();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Test"] = "abc";
        httpContext.Items[McpConstants.McpTransportName] = "http-sse";

        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        CallToolExecutionContext executionContext = CallToolExecutionContextHelper.CreateExecutionContext(httpContextAccessor: accessor);
        ITriggerData triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        var toolInvocationContext = (ToolInvocationContext)triggerData.BindingData["mcptoolcontext"];
        Assert.Equal("http-sse", toolInvocationContext.Transport!.Name);

        var headers = Assert.IsType<Dictionary<string, string>>(toolInvocationContext.Transport.Properties["headers"]);
        Assert.Equal("abc", headers["X-Test"]);
    }

    [Fact]
    public async Task BindAsync_InvalidValue_Throws()
    {
        var (binding, _) = CreateBinding();
        await Assert.ThrowsAsync<InvalidOperationException>(() => binding.BindAsync("wrong", CreateValueBindingContext()));
    }

    [Fact]
    public void BindingDataContract_ContainsExpectedKeys()
    {
        var (binding, param) = CreateBinding();
        Assert.Contains("mcptoolcontext", binding.BindingDataContract.Keys);
        Assert.Contains("mcptoolargs", binding.BindingDataContract.Keys);
        Assert.Contains("mcpsessionid", binding.BindingDataContract.Keys);
        Assert.Contains(param.Name!, binding.BindingDataContract.Keys);
        Assert.Contains("$return", binding.BindingDataContract.Keys);
    }

    [Fact]
    public async Task BindAsync_BindingData_IsCaseInsensitive()
    {
        var (binding, param) = CreateBinding();
        CallToolExecutionContext executionContext = CallToolExecutionContextHelper.CreateExecutionContext();
        var triggerData = await binding.BindAsync(executionContext, CreateValueBindingContext());

        // Access using different casings
        var ctxLower = triggerData.BindingData["mcptoolcontext"];
        var ctxMixed = triggerData.BindingData["McpToolContext"];
        var ctxUpper = triggerData.BindingData["MCPTOOLCONTEXT"];

        Assert.Same(ctxLower, ctxMixed);
        Assert.Same(ctxLower, ctxUpper);

        // Parameter name
        var paramValueOriginal = triggerData.BindingData[param.Name!];
        var paramValueUpper = triggerData.BindingData[param.Name!.ToUpperInvariant()];
        Assert.Same(paramValueOriginal, paramValueUpper);

        // mcptoolargs and mcpsessionid
        Assert.NotNull(triggerData.BindingData["McpToolArgs"]);
        Assert.NotNull(triggerData.BindingData["McpSessionId"]);
    }

    [Fact]
    public void BindingDataContract_IsCaseInsensitive()
    {
        var (binding, param) = CreateBinding();

        // Verify case-insensitive ContainsKey behavior
        Assert.True(binding.BindingDataContract.ContainsKey("McpToolContext"));
        Assert.True(binding.BindingDataContract.ContainsKey("McpToolArgs"));
        Assert.True(binding.BindingDataContract.ContainsKey("McpSessionId"));
        Assert.True(binding.BindingDataContract.ContainsKey(param.Name!.ToUpperInvariant()));
        Assert.True(binding.BindingDataContract.ContainsKey("$RETURN")); // different case
    }

    [Fact]
    public void GetInputSchema_WithValidSchema_ReturnsJsonDocument()
    {
        // Arrange
        var validSchema = """
            {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string",
                        "description": "The name parameter"
                    },
                    "age": {
                        "type": "number",
                        "description": "The age parameter"
                    }
                },
                "required": ["name"]
            }
            """;
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = validSchema
        };

        // Act
        var result = McpToolTriggerBinding.GetInputSchema(attribute);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("object", result.RootElement.GetProperty("type").GetString());
        Assert.True(result.RootElement.TryGetProperty("properties", out var propertiesElement));
        Assert.True(propertiesElement.TryGetProperty("name", out var nameProperty));
        Assert.Equal("string", nameProperty.GetProperty("type").GetString());
        
        // Clean up
        result.Dispose();
    }

    [Fact]
    public void GetInputSchema_WithInvalidJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidJson = """
            {
                "type": "object",
                "properties": {
                    "name": "string" // Missing closing brace and invalid structure
            """;
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = invalidJson
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => McpToolTriggerBinding.GetInputSchema(attribute));
        Assert.Contains("Failed to parse InputSchema for tool 'TestTool'", ex.Message);
        Assert.Contains("Schema must be valid JSON", ex.Message);
    }

    [Fact]
    public void GetInputSchema_WithMalformedJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var malformedJson = """
            {
                "type": "object",,
                "properties": {
                    "name": {
                        "type": "string"
                    }
                }
            }
            """;
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = malformedJson
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => McpToolTriggerBinding.GetInputSchema(attribute));
        Assert.Contains("Failed to parse InputSchema for tool 'TestTool'", ex.Message);
    }

    [Fact]
    public void GetInputSchema_WithEmptyString_ReturnsNull()
    {
        // Arrange
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = ""
        };

        // Act
        var result = McpToolTriggerBinding.GetInputSchema(attribute);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetInputSchema_WithNullInputSchema_ReturnsNull()
    {
        // Arrange
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = null
        };

        // Act
        var result = McpToolTriggerBinding.GetInputSchema(attribute);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetInputSchema_WithWhitespaceOnlyString_ReturnsNull()
    {
        // Arrange
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = "   \t\n   "
        };

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(() => McpToolTriggerBinding.GetInputSchema(attribute));
        Assert.Contains("Failed to parse InputSchema for tool 'TestTool'", ex.Message);
        Assert.Contains("Schema must be valid JSON", ex.Message);
    }

    [Fact]
    public void GetInputSchema_WithNonObjectSchema_ThrowsArgumentException()
    {
        // Arrange - Array schema is valid JSON but not a valid MCP tool schema
        var arraySchema = """
            [
                {
                    "type": "string"
                }
            ]
            """;
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = arraySchema
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => McpToolTriggerBinding.GetInputSchema(attribute));
        Assert.Contains("The specified document is not a valid MCP tool input JSON schema", ex.Message);
    }

    [Fact]
    public void GetInputSchema_WithStringSchema_ThrowsArgumentException()
    {
        // Arrange - String schema is valid JSON but not a valid MCP tool schema
        var stringSchema = "\"simple string\"";
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = stringSchema
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => McpToolTriggerBinding.GetInputSchema(attribute));
        Assert.Contains("The specified document is not a valid MCP tool input JSON schema", ex.Message);
    }

    [Fact]
    public void GetInputSchema_WithComplexValidSchema_ParsesCorrectly()
    {
        // Arrange
        var complexSchema = """
            {
                "type": "object",
                "properties": {
                    "user": {
                        "type": "object",
                        "properties": {
                            "name": {
                                "type": "string",
                                "minLength": 1,
                                "maxLength": 100
                            },
                            "email": {
                                "type": "string",
                                "format": "email"
                            },
                            "age": {
                                "type": "integer",
                                "minimum": 0,
                                "maximum": 120
                            },
                            "tags": {
                                "type": "array",
                                "items": {
                                    "type": "string"
                                },
                                "uniqueItems": true
                            },
                            "preferences": {
                                "type": "object",
                                "additionalProperties": true
                            }
                        },
                        "required": ["name", "email"]
                    },
                    "action": {
                        "type": "string",
                        "enum": ["create", "update", "delete"]
                    }
                },
                "required": ["user", "action"]
            }
            """;
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = complexSchema
        };

        // Act
        var result = McpToolTriggerBinding.GetInputSchema(attribute);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("object", result.RootElement.GetProperty("type").GetString());
        
        // Verify nested structure
        var propertiesElement = result.RootElement.GetProperty("properties");
        Assert.True(propertiesElement.TryGetProperty("user", out var userProperty));
        Assert.Equal("object", userProperty.GetProperty("type").GetString());
        
        var userPropertiesElement = userProperty.GetProperty("properties");
        Assert.True(userPropertiesElement.TryGetProperty("name", out var nameProperty));
        Assert.Equal("string", nameProperty.GetProperty("type").GetString());
        Assert.Equal(1, nameProperty.GetProperty("minLength").GetInt32());
        Assert.Equal(100, nameProperty.GetProperty("maxLength").GetInt32());
        
        // Verify array enum
        Assert.True(propertiesElement.TryGetProperty("action", out var actionProperty));
        Assert.Equal("string", actionProperty.GetProperty("type").GetString());
        var enumValues = actionProperty.GetProperty("enum").EnumerateArray().ToList();
        Assert.Equal(3, enumValues.Count);
        Assert.Contains(enumValues, e => e.GetString() == "create");
        Assert.Contains(enumValues, e => e.GetString() == "update");
        Assert.Contains(enumValues, e => e.GetString() == "delete");
        
        // Clean up
        result.Dispose();
    }

    [Fact]
    public void GetInputSchema_WithJsonWithTrailingComma_ThrowsInvalidOperationException()
    {
        // Arrange - JSON with trailing comma (invalid JSON)
        var invalidJson = """
            {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string"
                    },
                },
            }
            """;
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = invalidJson
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => McpToolTriggerBinding.GetInputSchema(attribute));
        Assert.Contains("Failed to parse InputSchema for tool 'TestTool'", ex.Message);
    }

    [Fact]
    public void GetInputSchema_WithJsonWithComments_ThrowsInvalidOperationException()
    {
        // Arrange - JSON with comments (invalid standard JSON)
        var jsonWithComments = """
            {
                // This is a comment
                "type": "object",
                "properties": {
                    /* Block comment */
                    "name": {
                        "type": "string"
                    }
                }
            }
            """;
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = jsonWithComments
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => McpToolTriggerBinding.GetInputSchema(attribute));
        Assert.Contains("Failed to parse InputSchema for tool 'TestTool'", ex.Message);
    }

    [Fact]
    public void GetInputSchema_WithUnbalancedBraces_ThrowsInvalidOperationException()
    {
        // Arrange
        var unbalancedJson = """
            {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string"
                    }
                }
            """; // Missing closing brace
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = unbalancedJson
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => McpToolTriggerBinding.GetInputSchema(attribute));
        Assert.Contains("Failed to parse InputSchema for tool 'TestTool'", ex.Message);
    }

    [Fact]
    public void GetInputSchema_WithEscapedCharacters_ParsesCorrectly()
    {
        // Arrange
        var schemaWithEscapedChars = """
            {
                "type": "object",
                "properties": {
                    "message": {
                        "type": "string",
                        "description": "A message with \"quotes\" and \\ backslashes\nand newlines\ttabs"
                    }
                }
            }
            """;
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = schemaWithEscapedChars
        };

        // Act
        var result = McpToolTriggerBinding.GetInputSchema(attribute);

        // Assert
        Assert.NotNull(result);
        var propertiesElement = result.RootElement.GetProperty("properties");
        var messageProperty = propertiesElement.GetProperty("message");
        var description = messageProperty.GetProperty("description").GetString();
        Assert.Contains("\"quotes\"", description);
        Assert.Contains("\\", description);
        Assert.Contains("\n", description);
        Assert.Contains("\t", description);
        
        // Clean up
        result.Dispose();
    }

    [Fact]
    public void GetInputSchema_WithInvalidMcpSchemaType_ThrowsArgumentException()
    {
        // Arrange - Valid JSON but wrong type (not "object")
        var invalidTypeSchema = """
            {
                "type": "string",
                "properties": {
                    "name": {
                        "type": "string"
                    }
                }
            }
            """;
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = invalidTypeSchema
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => McpToolTriggerBinding.GetInputSchema(attribute));
        Assert.Contains("The specified document is not a valid MCP tool input JSON schema", ex.Message);
    }

    [Fact]
    public void GetInputSchema_WithMissingTypeProperty_ThrowsArgumentException()
    {
        // Arrange - Valid JSON object but missing required "type" property
        var missingTypeSchema = """
            {
                "properties": {
                    "name": {
                        "type": "string"
                    }
                },
                "required": ["name"]
            }
            """;
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = missingTypeSchema
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => McpToolTriggerBinding.GetInputSchema(attribute));
        Assert.Contains("The specified document is not a valid MCP tool input JSON schema", ex.Message);
    }

    [Fact]
    public void GetInputSchema_WithInvalidPropertiesType_ThrowsArgumentException()
    {
        // Arrange - Valid JSON but properties is not an object (it's an array)
        var invalidPropertiesSchema = """
            {
                "type": "object",
                "properties": [
                    {"name": "test"}
                ],
                "required": ["name"]
            }
            """;
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = invalidPropertiesSchema
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => McpToolTriggerBinding.GetInputSchema(attribute));
        Assert.Contains("The specified document is not a valid MCP tool input JSON schema", ex.Message);
    }

    [Fact]
    public void GetInputSchema_WithInvalidRequiredType_ThrowsArgumentException()
    {
        // Arrange - Valid JSON but required is not an array (it's an object)
        var invalidRequiredSchema = """
            {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string"
                    }
                },
                "required": {"name": true}
            }
            """;
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = invalidRequiredSchema
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => McpToolTriggerBinding.GetInputSchema(attribute));
        Assert.Contains("The specified document is not a valid MCP tool input JSON schema", ex.Message);
    }

    [Fact]
    public void GetInputSchema_WithValidSchemaNoProperties_ParsesCorrectly()
    {
        // Arrange - Valid minimal MCP schema with no properties
        var minimalSchema = """
            {
                "type": "object"
            }
            """;
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = minimalSchema
        };

        // Act
        var result = McpToolTriggerBinding.GetInputSchema(attribute);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("object", result.RootElement.GetProperty("type").GetString());
        Assert.False(result.RootElement.TryGetProperty("properties", out _));
        Assert.False(result.RootElement.TryGetProperty("required", out _));
        
        // Clean up
        result.Dispose();
    }

    [Fact]
    public void GetInputSchema_WithValidSchemaEmptyProperties_ParsesCorrectly()
    {
        // Arrange - Valid MCP schema with empty properties object
        var emptyPropertiesSchema = """
            {
                "type": "object",
                "properties": {},
                "required": []
            }
            """;
        
        var attribute = new McpToolTriggerAttribute("TestTool", "Test Description")
        {
            InputSchema = emptyPropertiesSchema
        };

        // Act
        var result = McpToolTriggerBinding.GetInputSchema(attribute);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("object", result.RootElement.GetProperty("type").GetString());
        Assert.True(result.RootElement.TryGetProperty("properties", out var propertiesElement));
        Assert.Equal(JsonValueKind.Object, propertiesElement.ValueKind);
        Assert.Empty(propertiesElement.EnumerateObject());
        Assert.True(result.RootElement.TryGetProperty("required", out var required));
        Assert.Equal(JsonValueKind.Array, required.ValueKind);
        Assert.Empty(required.EnumerateArray());
        
        // Clean up
        result.Dispose();
    }
}
