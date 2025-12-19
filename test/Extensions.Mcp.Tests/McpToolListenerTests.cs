// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Validation;
using Microsoft.Azure.WebJobs.Host.Executors;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;
public class McpToolListenerTests
{
    private static JsonElement CreateFromJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static IMcpToolProperty CreateProperty(string name, bool required)
    {
        var mock = new Mock<IMcpToolProperty>();
        mock.SetupAllProperties();
        mock.Object.PropertyName = name;
        mock.Object.IsRequired = required;
        return mock.Object;
    }

    private static RequestContext<CallToolRequestParams> CreateRequest(params (string key, JsonElement value)[] args)
    {
        var dict = args?.ToDictionary(x => x.key, x => x.value) ?? new Dictionary<string, JsonElement>();
        var server = new Mock<McpServer>().Object;
        var parameters = new CallToolRequestParams { Name = "params", Arguments = dict };

        return new RequestContext<CallToolRequestParams>(server, new JsonRpcRequest() { Method = RequestMethods.ToolsCall })
        {
            Params = parameters
        };
    }

    private static CallToolRequestParams CreateRequestParams(params (string key, JsonElement value)[] args)
    {
        var dict = args?.ToDictionary(x => x.key, x => x.value) ?? new Dictionary<string, JsonElement>();
        return new CallToolRequestParams { Name = "testTool", Arguments = dict };
    }

    [Fact]
    public async Task RunAsync_Throws_WhenRequiredPropertyMissing()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var properties = new[] { CreateProperty("foo", true) };
        var validator = new PropertyBasedToolRequestValidator(properties);
        var listener = new McpToolListener(executor, "func", "tool", null, validator);

        var request = CreateRequest(); // No arguments

        var ex = await Assert.ThrowsAsync<McpProtocolException>(() =>
            listener.RunAsync(request, CancellationToken.None));
        Assert.Contains("One or more required tool properties are missing values. Please provide: foo", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_Throws_WhenRequiredPropertyIsNull()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var properties = new[] { CreateProperty("foo", true) };
        var validator = new PropertyBasedToolRequestValidator(properties);
        var listener = new McpToolListener(executor, "func", "tool", null, validator);

        var request = CreateRequest(("foo", JsonDocument.Parse("null").RootElement));

        var ex = await Assert.ThrowsAsync<McpProtocolException>(() =>
            listener.RunAsync(request, CancellationToken.None));
        Assert.Contains("One or more required tool properties are missing values. Please provide: foo", ex.Message);
    }

    [Fact]
    public void PropertyBasedValidator_Throws_WhenRequiredPropertyMissing()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var validator = new PropertyBasedToolRequestValidator(properties);
        var request = CreateRequestParams(); // No arguments

        var ex = Assert.Throws<McpProtocolException>(() => validator.Validate(request));
        Assert.Contains("One or more required tool properties are missing values. Please provide: foo", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public void PropertyBasedValidator_Throws_WhenRequiredPropertyIsNull()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var validator = new PropertyBasedToolRequestValidator(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("null").RootElement));

        var ex = Assert.Throws<McpProtocolException>(() => validator.Validate(request));
        Assert.Contains("One or more required tool properties are missing values. Please provide: foo", ex.Message);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyIsEmptyString()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var validator = new PropertyBasedToolRequestValidator(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("\"\"").RootElement));

        var ex = Record.Exception(() => validator.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyPresentAndValid()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var validator = new PropertyBasedToolRequestValidator(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("\"bar\"").RootElement));

        var ex = Record.Exception(() => validator.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyIsEmptyArray()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var validator = new PropertyBasedToolRequestValidator(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("[]").RootElement));

        var ex = Record.Exception(() => validator.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyIsNonEmptyArray()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var validator = new PropertyBasedToolRequestValidator(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("[1]").RootElement));

        var ex = Record.Exception(() => validator.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyIsEmptyObject()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var validator = new PropertyBasedToolRequestValidator(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("{}").RootElement));

        var ex = Record.Exception(() => validator.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyIsNonEmptyObject()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var validator = new PropertyBasedToolRequestValidator(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("{\"bar\":1}").RootElement));

        var ex = Record.Exception(() => validator.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyIsNumber()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var validator = new PropertyBasedToolRequestValidator(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("123").RootElement));

        var ex = Record.Exception(() => validator.Validate(request));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyIsBoolean(string boolValue)
    {
        var properties = new[] { CreateProperty("foo", true) };
        var validator = new PropertyBasedToolRequestValidator(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse(boolValue).RootElement));

        var ex = Record.Exception(() => validator.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void JsonSchemaValidator_UsesInputSchema_WhenAvailable()
    {
        // Test JsonSchemaValidator with input schema
        var inputSchemaJson = """
            {
                "type": "object",
                "required": ["propFromSchema"]
            }
            """;
        var inputSchema = CreateFromJson(inputSchemaJson);
        var validator = new JsonSchemaToolRequestValidator(inputSchema);
        var request = CreateRequestParams(); // No arguments

        var ex = Assert.Throws<McpProtocolException>(() => validator.Validate(request));
        
        // Should use schema's required properties
        Assert.Contains("One or more required tool properties are missing values. Please provide: propFromSchema", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public void PropertyBasedValidator_FallsBackToProperties_WhenNoSchema()
    {
        // Test PropertyBasedValidator
        var properties = new[] { CreateProperty("propFromProperty", true) };
        var validator = new PropertyBasedToolRequestValidator(properties);
        var request = CreateRequestParams(); // No arguments

        var ex = Assert.Throws<McpProtocolException>(() => validator.Validate(request));
        
        // Should use properties
        Assert.Contains("One or more required tool properties are missing values. Please provide: propFromProperty", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public void JsonSchemaValidator_DoesNotThrow_WhenSchemaRequiredPropertyPresent()
    {
        // Test JsonSchemaValidator when required property is present
        var inputSchemaJson = """
            {
                "type": "object",
                "required": ["propFromSchema"]
            }
            """;
        var inputSchema = CreateFromJson(inputSchemaJson);
        var validator = new JsonSchemaToolRequestValidator(inputSchema);
        var request = CreateRequestParams(("propFromSchema", JsonDocument.Parse("\"value\"").RootElement));

        var ex = Record.Exception(() => validator.Validate(request));
        
        Assert.Null(ex);
    }

    [Fact]
    public void JsonSchemaValidator_DoesNotThrow_WhenNoRequiredPropertiesInSchema()
    {
        // Test JsonSchemaValidator with empty required array
        var inputSchemaJson = """
            {
                "type": "object",
                "required": []
            }
            """;
        var inputSchema = CreateFromJson(inputSchemaJson);
        var validator = new JsonSchemaToolRequestValidator(inputSchema);
        var request = CreateRequestParams(); // No arguments

        var ex = Record.Exception(() => validator.Validate(request));
        
        Assert.Null(ex);
    }

    [Fact]
    public void JsonSchemaValidator_IgnoresToolProperties_WhenInputSchemaProvided()
    {
        // This test demonstrates that JsonSchemaValidator only uses schema properties
        var inputSchemaJson = """
            {
                "type": "object",
                "required": ["schemaProp1", "schemaProp2"]
            }
            """;
        var inputSchema = CreateFromJson(inputSchemaJson);
        var validator = new JsonSchemaToolRequestValidator(inputSchema);
        
        // Provide arguments that would satisfy tool properties but not schema properties
        var request = CreateRequestParams(
            ("toolProp1", JsonDocument.Parse("\"value1\"").RootElement),
            ("toolProp2", JsonDocument.Parse("\"value2\"").RootElement));

        // Act & Assert - Should validate against input schema only
        var ex = Assert.Throws<McpProtocolException>(() => validator.Validate(request));
        
        // Should complain about missing schema properties
        Assert.Contains("schemaProp1", ex.Message);
        Assert.Contains("schemaProp2", ex.Message);
    }

    [Fact]
    public void JsonSchemaValidator_ValidatesCorrectly_WhenOnlyInputSchemaUsed()
    {
        // Test JsonSchemaValidator best practice
        var inputSchemaJson = """
            {
                "type": "object",
                "required": ["actualRequiredProp"]
            }
            """;
        var inputSchema = CreateFromJson(inputSchemaJson);
        var validator = new JsonSchemaToolRequestValidator(inputSchema);
        var request = CreateRequestParams(("actualRequiredProp", JsonDocument.Parse("\"correctValue\"").RootElement));

        // Act & Assert - Should validate successfully using only input schema
        var ex = Record.Exception(() => validator.Validate(request));
        
        Assert.Null(ex); // Should not throw because input schema's required property is provided
    }

    [Fact]
    public void McpToolListener_Constructor_WorksWithPropertyBasedValidator()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        
        // Test with tool properties validator
        var propertiesOnly = new[] { CreateProperty("prop1", true) };
        var validator = new PropertyBasedToolRequestValidator(propertiesOnly);
        var listener = new McpToolListener(executor, "func", "tool", null, validator);
        listener.Properties = propertiesOnly; // Set for interface compatibility
        
        Assert.NotNull(listener.Properties);
        Assert.Single(listener.Properties);
        Assert.Null(listener.InputSchema);
    }

    [Fact]
    public void McpToolListener_Constructor_WorksWithJsonSchemaValidator()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        
        // Test with input schema validator
        var inputSchemaJson = """
            {
                "type": "object",
                "required": ["prop1"]
            }
            """;
        var inputSchemaOnly = CreateFromJson(inputSchemaJson);
        var validator = new JsonSchemaToolRequestValidator(inputSchemaOnly);
        var listener = new McpToolListener(executor, "func", "tool", null, validator);
        listener.InputSchema = inputSchemaOnly; // Set for interface compatibility
        
        Assert.Empty(listener.Properties);
        Assert.NotNull(listener.InputSchema);
    }

    [Fact]
    public void Validators_WorkCorrectlyForBothPatterns()
    {
        // Test that both validator types work correctly
        var emptyRequest = CreateRequestParams();

        // Traditional approach validation
        var toolPropertiesOnly = new[] { CreateProperty("traditionalProp", true) };
        var propertyValidator = new PropertyBasedToolRequestValidator(toolPropertiesOnly);
        var traditionalEx = Assert.Throws<McpProtocolException>(() => propertyValidator.Validate(emptyRequest));
        Assert.Contains("traditionalProp", traditionalEx.Message);

        // Modern approach validation  
        var inputSchemaJson = """
            {
                "type": "object",
                "properties": {
                    "modernProp": {
                        "type": "string",
                        "description": "Modern property"
                    }
                },
                "required": ["modernProp"]
            }
            """;
        var inputSchemaOnly = CreateFromJson(inputSchemaJson);
        var schemaValidator = new JsonSchemaToolRequestValidator(inputSchemaOnly);
        var modernEx = Assert.Throws<McpProtocolException>(() => schemaValidator.Validate(emptyRequest));
        Assert.Contains("modernProp", modernEx.Message);
    }

    [Fact]
    public async Task RunAsync_WithJsonSchemaValidator_ThrowsWhenRequiredPropertyMissing()
    {
        // Arrange
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var schemaJson = """
            {
                "type": "object",
                "required": ["schemaRequiredProp"]
            }
            """;
        var schema = CreateFromJson(schemaJson);
        var validator = new JsonSchemaToolRequestValidator(schema);
        var listener = new McpToolListener(executor, "func", "tool", null, validator);

        var request = CreateRequest(); // No arguments

        // Act & Assert
        var ex = await Assert.ThrowsAsync<McpProtocolException>(() =>
            listener.RunAsync(request, CancellationToken.None));
        Assert.Contains("schemaRequiredProp", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_WithJsonSchemaValidator_DoesNotThrowWhenRequiredPropertyPresent()
    {
        // Arrange
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var schemaJson = """
            {
                "type": "object",
                "required": ["schemaRequiredProp"]
            }
            """;
        var schema = CreateFromJson(schemaJson);
        var validator = new JsonSchemaToolRequestValidator(schema);
        var listener = new McpToolListener(executor, "func", "tool", null, validator);

        var request = CreateRequest(("schemaRequiredProp", JsonDocument.Parse("\"value\"").RootElement));

        // Mock the executor to avoid actual function execution
        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock.Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new FunctionResult(true));

        var listenerWithMock = new McpToolListener(executorMock.Object, "func", "tool", null, validator);

        // Act - should not throw
        var result = await listenerWithMock.RunAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Content); // Empty content since we mocked the execution
    }
}
