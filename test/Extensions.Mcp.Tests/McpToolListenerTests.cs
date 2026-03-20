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
        var inputSchema = new PropertyBasedToolInputSchema(properties);
        var metadata = new Dictionary<string, object?>();
        var listener = new McpToolListener(executor, "func", "tool", null, inputSchema, metadata);

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
        var inputSchema = new PropertyBasedToolInputSchema(properties);
        var metadata = new Dictionary<string, object?>();
        var listener = new McpToolListener(executor, "func", "tool", null, inputSchema, metadata);

        var request = CreateRequest(("foo", JsonDocument.Parse("null").RootElement));

        var ex = await Assert.ThrowsAsync<McpProtocolException>(() =>
            listener.RunAsync(request, CancellationToken.None));
        Assert.Contains("One or more required tool properties are missing values. Please provide: foo", ex.Message);
    }

    [Fact]
    public void PropertyBasedValidator_Throws_WhenRequiredPropertyMissing()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var inputSchema = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequestParams(); // No arguments

        var ex = Assert.Throws<McpProtocolException>(() => inputSchema.Validate(request));
        Assert.Contains("One or more required tool properties are missing values. Please provide: foo", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public void PropertyBasedValidator_Throws_WhenRequiredPropertyIsNull()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var inputSchema = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("null").RootElement));

        var ex = Assert.Throws<McpProtocolException>(() => inputSchema.Validate(request));
        Assert.Contains("One or more required tool properties are missing values. Please provide: foo", ex.Message);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyIsEmptyString()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var inputSchema = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("\"\"").RootElement));

        var ex = Record.Exception(() => inputSchema.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyPresentAndValid()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var inputSchema = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("\"bar\"").RootElement));

        var ex = Record.Exception(() => inputSchema.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyIsEmptyArray()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var inputSchema = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("[]").RootElement));

        var ex = Record.Exception(() => inputSchema.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyIsNonEmptyArray()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var inputSchema = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("[1]").RootElement));

        var ex = Record.Exception(() => inputSchema.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyIsEmptyObject()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var inputSchema = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("{}").RootElement));

        var ex = Record.Exception(() => inputSchema.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyIsNonEmptyObject()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var inputSchema = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("{\"bar\":1}").RootElement));

        var ex = Record.Exception(() => inputSchema.Validate(request));
        Assert.Null(ex);
    }

    [Fact]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyIsNumber()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var inputSchema = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse("123").RootElement));

        var ex = Record.Exception(() => inputSchema.Validate(request));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void PropertyBasedValidator_DoesNotThrow_WhenRequiredPropertyIsBoolean(string boolValue)
    {
        var properties = new[] { CreateProperty("foo", true) };
        var inputSchema = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequestParams(("foo", JsonDocument.Parse(boolValue).RootElement));

        var ex = Record.Exception(() => inputSchema.Validate(request));
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
        var jsonDoc = JsonDocument.Parse(inputSchemaJson);
        var inputSchema = new JsonSchemaToolInputSchema(jsonDoc);
        var request = CreateRequestParams(); // No arguments

        var ex = Assert.Throws<McpProtocolException>(() => inputSchema.Validate(request));
        
        // Should use schema's required properties
        Assert.Contains("One or more required tool properties are missing values. Please provide: propFromSchema", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public void PropertyBasedValidator_FallsBackToProperties_WhenNoSchema()
    {
        // Test PropertyBasedValidator
        var properties = new[] { CreateProperty("propFromProperty", true) };
        var inputSchema = new PropertyBasedToolInputSchema(properties);
        var request = CreateRequestParams(); // No arguments

        var ex = Assert.Throws<McpProtocolException>(() => inputSchema.Validate(request));
        
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
        var jsonDoc = JsonDocument.Parse(inputSchemaJson);
        var inputSchema = new JsonSchemaToolInputSchema(jsonDoc);
        var request = CreateRequestParams(("propFromSchema", JsonDocument.Parse("\"value\"").RootElement));

        var ex = Record.Exception(() => inputSchema.Validate(request));
        
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
        var jsonDoc = JsonDocument.Parse(inputSchemaJson);
        var inputSchema = new JsonSchemaToolInputSchema(jsonDoc);
        var request = CreateRequestParams(); // No arguments

        var ex = Record.Exception(() => inputSchema.Validate(request));
        
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
        var jsonDoc = JsonDocument.Parse(inputSchemaJson);
        var inputSchema = new JsonSchemaToolInputSchema(jsonDoc);
        
        // Provide arguments that would satisfy tool properties but not schema properties
        var request = CreateRequestParams(
            ("toolProp1", JsonDocument.Parse("\"value1\"").RootElement),
            ("toolProp2", JsonDocument.Parse("\"value2\"").RootElement));

        // Act & Assert - Should validate against input schema only
        var ex = Assert.Throws<McpProtocolException>(() => inputSchema.Validate(request));
        
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
        var jsonDoc = JsonDocument.Parse(inputSchemaJson);
        var inputSchema = new JsonSchemaToolInputSchema(jsonDoc);
        var request = CreateRequestParams(("actualRequiredProp", JsonDocument.Parse("\"correctValue\"").RootElement));

        // Act & Assert - Should validate successfully using only input schema
        var ex = Record.Exception(() => inputSchema.Validate(request));
        
        Assert.Null(ex); // Should not throw because input schema's required property is provided
    }

    [Fact]
    public void McpToolListener_Constructor_WorksWithPropertyBasedValidator()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        
        // Test with tool properties validator
        var propertiesOnly = new[] { CreateProperty("prop1", true) };
        var inputSchema = new PropertyBasedToolInputSchema(propertiesOnly);
        var metadata = new Dictionary<string, object?>();
        var listener = new McpToolListener(executor, "func", "tool", null, inputSchema, metadata);
        
        Assert.NotNull(listener.InputSchema);
        
        // Verify the schema can generate a proper JsonElement
        var schemaElement = listener.InputSchema.GetSchemaElement();
        Assert.Equal("object", schemaElement.GetProperty("type").GetString());
        Assert.True(schemaElement.TryGetProperty("properties", out var properties));
        Assert.True(properties.TryGetProperty("prop1", out var _));
    }

    [Fact]
    public void McpToolListener_Constructor_WorksWithJsonSchemaValidator()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        
        // Test with input schema validator
        var inputSchemaJson = """
            {
                "type": "object",
                "properties": {
                    "prop1": {
                        "type": "string"
                    }
                },
                "required": ["prop1"]
            }
            """;
        var jsonDoc = JsonDocument.Parse(inputSchemaJson);
        var inputSchema = new JsonSchemaToolInputSchema(jsonDoc);
        var metadata = new Dictionary<string, object?>();
        var listener = new McpToolListener(executor, "func", "tool", null, inputSchema, metadata);
        
        Assert.NotNull(listener.InputSchema);
        
        // Verify the schema can generate a proper JsonElement
        var schemaElement = listener.InputSchema.GetSchemaElement();
        Assert.Equal("object", schemaElement.GetProperty("type").GetString());
        Assert.True(schemaElement.TryGetProperty("properties", out var properties));
        Assert.True(properties.TryGetProperty("prop1", out var prop1));
        Assert.Equal("string", prop1.GetProperty("type").GetString());
        
        var required = schemaElement.GetProperty("required");
        Assert.Single(required.EnumerateArray());
        Assert.Equal("prop1", required[0].GetString());
    }

    [Fact]
    public void Validators_WorkCorrectlyForBothPatterns()
    {
        // Test that both validator types work correctly
        var emptyRequest = CreateRequestParams();

        // Traditional approach validation
        var toolPropertiesOnly = new[] { CreateProperty("traditionalProp", true) };
        var propertyInputSchema = new PropertyBasedToolInputSchema(toolPropertiesOnly);
        var traditionalEx = Assert.Throws<McpProtocolException>(() => propertyInputSchema.Validate(emptyRequest));
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
        var jsonDoc = JsonDocument.Parse(inputSchemaJson);
        var jsonInputSchema = new JsonSchemaToolInputSchema(jsonDoc);
        var modernEx = Assert.Throws<McpProtocolException>(() => jsonInputSchema.Validate(emptyRequest));
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
        var jsonDoc = JsonDocument.Parse(schemaJson);
        var inputSchema = new JsonSchemaToolInputSchema(jsonDoc);
        var metadata = new Dictionary<string, object?>();
        var listener = new McpToolListener(executor, "func", "tool", null, inputSchema, metadata);

        var request = CreateRequest(); // No arguments

        // Act & Assert
        var ex = await Assert.ThrowsAsync<McpProtocolException>(() =>
            listener.RunAsync(request, CancellationToken.None));
        Assert.Contains("schemaRequiredProp", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public void RunAsync_WithJsonSchemaValidator_DoesNotThrowWhenRequiredPropertyPresent()
    {
        // Arrange
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var schemaJson = """
            {
                "type": "object",
                "required": ["schemaRequiredProp"]
            }
            """;
        var jsonDoc = JsonDocument.Parse(schemaJson);
        var inputSchema = new JsonSchemaToolInputSchema(jsonDoc);
        var metadata = new Dictionary<string, object?>();
        var listener = new McpToolListener(executor, "func", "tool", null, inputSchema, metadata);

        var request = CreateRequestParams(("schemaRequiredProp", JsonDocument.Parse("\"value\"").RootElement));

        var ex = Record.Exception(() => inputSchema.Validate(request));

        Assert.Null(ex); // Should not throw because input schema's required property is provided
    }

    [Fact]
    public void Metadata_WithValues_ReturnsMetadata()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var inputSchema = new PropertyBasedToolInputSchema([]);
        var metadata = new Dictionary<string, object?>
        {
            ["author"] = "Jane Doe",
            ["version"] = 1.0
        };
        var listener = new McpToolListener(executor, "func", "tool", "description", inputSchema, metadata);

        Assert.NotNull(listener.Metadata);
        Assert.Equal("Jane Doe", listener.Metadata["author"]);
        Assert.Equal(1.0, listener.Metadata["version"]);
    }

    [Fact]
    public void Metadata_WithNestedValues_ReturnsNestedMetadata()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var inputSchema = new PropertyBasedToolInputSchema([]);
        var metadata = new Dictionary<string, object?>
        {
            ["ui"] = new Dictionary<string, object?>
            {
                ["resourceUri"] = "ui://test/widget",
                ["prefersBorder"] = true
            }
        };
        var listener = new McpToolListener(executor, "func", "tool", "description", inputSchema, metadata);

        Assert.NotNull(listener.Metadata);
        var ui = Assert.IsType<Dictionary<string, object?>>(listener.Metadata["ui"]);
        Assert.Equal("ui://test/widget", ui["resourceUri"]);
        Assert.Equal(true, ui["prefersBorder"]);
    }

    [Fact]
    public void Metadata_WhenEmpty_ReturnsEmptyDictionary()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var inputSchema = new PropertyBasedToolInputSchema([]);
        var metadata = new Dictionary<string, object?>();
        var listener = new McpToolListener(executor, "func", "tool", null, inputSchema, metadata);

        Assert.NotNull(listener.Metadata);
        Assert.Empty(listener.Metadata);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_Throws_WhenStructuredContentMissing()
    {
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "hello" }]
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var inputSchema = new PropertyBasedToolInputSchema([]);
        var metadata = new Dictionary<string, object?>();
        var outputSchema = JsonDocument.Parse("""{"type":"object","properties":{"name":{"type":"string"}}}""").RootElement;

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, inputSchema, metadata, outputSchema);
        var request = CreateRequest();

        var ex = await Assert.ThrowsAsync<McpProtocolException>(() =>
            listener.RunAsync(request, CancellationToken.None));
        Assert.Contains("does not contain structured content", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_Throws_WhenRequiredPropertyMissing()
    {
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "hello" }],
            StructuredContent = new System.Text.Json.Nodes.JsonObject
            {
                ["other"] = "value"
            }
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var inputSchema = new PropertyBasedToolInputSchema([]);
        var metadata = new Dictionary<string, object?>();
        var outputSchema = JsonDocument.Parse("""{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}""").RootElement;

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, inputSchema, metadata, outputSchema);
        var request = CreateRequest();

        var ex = await Assert.ThrowsAsync<McpProtocolException>(() =>
            listener.RunAsync(request, CancellationToken.None));
        Assert.Contains("output schema", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_Throws_WhenRequiredPropertyIsNull()
    {
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "hello" }],
            StructuredContent = new System.Text.Json.Nodes.JsonObject
            {
                ["name"] = (System.Text.Json.Nodes.JsonNode?)null
            }
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var inputSchema = new PropertyBasedToolInputSchema([]);
        var metadata = new Dictionary<string, object?>();
        var outputSchema = JsonDocument.Parse("""{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}""").RootElement;

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, inputSchema, metadata, outputSchema);
        var request = CreateRequest();

        var ex = await Assert.ThrowsAsync<McpProtocolException>(() =>
            listener.RunAsync(request, CancellationToken.None));
        Assert.Contains("output schema", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_Succeeds_WhenStructuredContentValid()
    {
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = """{"name":"Alice","age":30}""" }],
            StructuredContent = new System.Text.Json.Nodes.JsonObject
            {
                ["name"] = "Alice",
                ["age"] = 30
            }
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var inputSchema = new PropertyBasedToolInputSchema([]);
        var metadata = new Dictionary<string, object?>();
        var outputSchema = JsonDocument.Parse("""{"type":"object","required":["name"],"properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""").RootElement;

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, inputSchema, metadata, outputSchema);
        var request = CreateRequest();

        var result = await listener.RunAsync(request, CancellationToken.None);
        Assert.Same(callToolResult, result);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_Succeeds_WhenNoRequiredProperties()
    {
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "{}" }],
            StructuredContent = new System.Text.Json.Nodes.JsonObject()
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var inputSchema = new PropertyBasedToolInputSchema([]);
        var metadata = new Dictionary<string, object?>();
        var outputSchema = JsonDocument.Parse("""{"type":"object","properties":{"name":{"type":"string"}}}""").RootElement;

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, inputSchema, metadata, outputSchema);
        var request = CreateRequest();

        var result = await listener.RunAsync(request, CancellationToken.None);
        Assert.Same(callToolResult, result);
    }

    [Fact]
    public async Task RunAsync_WithoutOutputSchema_DoesNotValidateStructuredContent()
    {
        // No output schema — structured content is not validated, even if missing.
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "hello" }]
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var inputSchema = new PropertyBasedToolInputSchema([]);
        var metadata = new Dictionary<string, object?>();

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, inputSchema, metadata);
        var request = CreateRequest();

        var result = await listener.RunAsync(request, CancellationToken.None);
        Assert.Same(callToolResult, result);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_Throws_WhenPropertyTypeMismatch()
    {
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "test" }],
            StructuredContent = new System.Text.Json.Nodes.JsonObject
            {
                ["name"] = 123 // should be string
            }
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var inputSchema = new PropertyBasedToolInputSchema([]);
        var metadata = new Dictionary<string, object?>();
        var outputSchema = JsonDocument.Parse("""{"type":"object","properties":{"name":{"type":"string"}}}""").RootElement;

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, inputSchema, metadata, outputSchema);
        var request = CreateRequest();

        var ex = await Assert.ThrowsAsync<McpProtocolException>(() =>
            listener.RunAsync(request, CancellationToken.None));
        Assert.Contains("output schema", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_Throws_WhenObjectExpectedButGotString()
    {
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "test" }],
            StructuredContent = new System.Text.Json.Nodes.JsonObject
            {
                ["address"] = "not an object"
            }
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var inputSchema = new PropertyBasedToolInputSchema([]);
        var metadata = new Dictionary<string, object?>();
        var outputSchema = JsonDocument.Parse("""{"type":"object","properties":{"address":{"type":"object"}}}""").RootElement;

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, inputSchema, metadata, outputSchema);
        var request = CreateRequest();

        var ex = await Assert.ThrowsAsync<McpProtocolException>(() =>
            listener.RunAsync(request, CancellationToken.None));
        Assert.Contains("output schema", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_Throws_WhenArrayExpectedButGotString()
    {
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "test" }],
            StructuredContent = new System.Text.Json.Nodes.JsonObject
            {
                ["tags"] = "not-an-array"
            }
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var inputSchema = new PropertyBasedToolInputSchema([]);
        var metadata = new Dictionary<string, object?>();
        var outputSchema = JsonDocument.Parse("""{"type":"object","properties":{"tags":{"type":"array"}}}""").RootElement;

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, inputSchema, metadata, outputSchema);
        var request = CreateRequest();

        var ex = await Assert.ThrowsAsync<McpProtocolException>(() =>
            listener.RunAsync(request, CancellationToken.None));
        Assert.Contains("output schema", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_Throws_WhenIntegerPropertyHasFractionalValue()
    {
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "test" }],
            StructuredContent = new System.Text.Json.Nodes.JsonObject
            {
                ["age"] = 3.14
            }
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var listener = new McpToolListener(
            executorMock.Object,
            "func",
            "tool",
            null,
            new PropertyBasedToolInputSchema([]),
            new Dictionary<string, object?>(),
            JsonDocument.Parse("""{"type":"object","properties":{"age":{"type":"integer"}}}""").RootElement);

        var ex = await Assert.ThrowsAsync<McpProtocolException>(() => listener.RunAsync(CreateRequest(), CancellationToken.None));
        Assert.Contains("output schema", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_Throws_WhenNestedObjectViolatesSchema()
    {
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "test" }],
            StructuredContent = new System.Text.Json.Nodes.JsonObject
            {
                ["address"] = new System.Text.Json.Nodes.JsonObject()
            }
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var outputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "address": {
                        "type": "object",
                        "properties": {
                            "city": { "type": "string" }
                        },
                        "required": ["city"]
                    }
                }
            }
            """).RootElement;

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, new PropertyBasedToolInputSchema([]), new Dictionary<string, object?>(), outputSchema);

        var ex = await Assert.ThrowsAsync<McpProtocolException>(() => listener.RunAsync(CreateRequest(), CancellationToken.None));
        Assert.Contains("output schema", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_Throws_WhenArrayItemsViolateSchema()
    {
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "test" }],
            StructuredContent = new System.Text.Json.Nodes.JsonObject
            {
                ["tags"] = new System.Text.Json.Nodes.JsonArray(1, 2)
            }
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var outputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "tags": {
                        "type": "array",
                        "items": { "type": "string" }
                    }
                }
            }
            """).RootElement;

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, new PropertyBasedToolInputSchema([]), new Dictionary<string, object?>(), outputSchema);

        var ex = await Assert.ThrowsAsync<McpProtocolException>(() => listener.RunAsync(CreateRequest(), CancellationToken.None));
        Assert.Contains("output schema", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_Throws_WhenAdditionalPropertiesAreForbidden()
    {
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "test" }],
            StructuredContent = new System.Text.Json.Nodes.JsonObject
            {
                ["name"] = "Alice",
                ["extra"] = true
            }
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var outputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": { "type": "string" }
                },
                "additionalProperties": false
            }
            """).RootElement;

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, new PropertyBasedToolInputSchema([]), new Dictionary<string, object?>(), outputSchema);

        var ex = await Assert.ThrowsAsync<McpProtocolException>(() => listener.RunAsync(CreateRequest(), CancellationToken.None));
        Assert.Contains("output schema", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_AllowsRequiredNullWhenSchemaIncludesNullType()
    {
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "{\"name\":null}" }],
            StructuredContent = new System.Text.Json.Nodes.JsonObject
            {
                ["name"] = (System.Text.Json.Nodes.JsonNode?)null
            }
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var outputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "required": ["name"],
                "properties": {
                    "name": {
                        "type": ["string", "null"]
                    }
                }
            }
            """).RootElement;

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, new PropertyBasedToolInputSchema([]), new Dictionary<string, object?>(), outputSchema);

        var result = await listener.RunAsync(CreateRequest(), CancellationToken.None);
        Assert.Same(callToolResult, result);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_Succeeds_WhenAllTypesMatch()
    {
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "test" }],
            StructuredContent = new System.Text.Json.Nodes.JsonObject
            {
                ["name"] = "Alice",
                ["age"] = 30,
                ["active"] = true,
                ["tags"] = new System.Text.Json.Nodes.JsonArray("a", "b"),
                ["address"] = new System.Text.Json.Nodes.JsonObject { ["city"] = "Seattle" }
            }
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var inputSchema = new PropertyBasedToolInputSchema([]);
        var metadata = new Dictionary<string, object?>();
        var outputSchema = JsonDocument.Parse("""
            {
                "type":"object",
                "properties":{
                    "name":{"type":"string"},
                    "age":{"type":"integer"},
                    "active":{"type":"boolean"},
                    "tags":{"type":"array"},
                    "address":{"type":"object"}
                }
            }
            """).RootElement;

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, inputSchema, metadata, outputSchema);
        var request = CreateRequest();

        var result = await listener.RunAsync(request, CancellationToken.None);
        Assert.Same(callToolResult, result);
    }

    [Fact]
    public async Task RunAsync_WithOutputSchema_Succeeds_WhenPropertyNotInSchema()
    {
        // Extra properties not declared in the schema should not cause validation errors.
        var callToolResult = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "test" }],
            StructuredContent = new System.Text.Json.Nodes.JsonObject
            {
                ["name"] = "Alice",
                ["extraProp"] = "should be fine"
            }
        };

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        executorMock
            .Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var ctx = (CallToolExecutionContext)data.TriggerValue;
                ctx.SetResult(callToolResult);
                return Task.FromResult(new FunctionResult(true));
            });

        var inputSchema = new PropertyBasedToolInputSchema([]);
        var metadata = new Dictionary<string, object?>();
        var outputSchema = JsonDocument.Parse("""{"type":"object","properties":{"name":{"type":"string"}}}""").RootElement;

        var listener = new McpToolListener(executorMock.Object, "func", "tool", null, inputSchema, metadata, outputSchema);
        var request = CreateRequest();

        var result = await listener.RunAsync(request, CancellationToken.None);
        Assert.Same(callToolResult, result);
    }
}
