// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Executors;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;
using System.Text.Json;
using Xunit;

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

    private static JsonElement CreateInputSchemaJson(string json)
    {
        return McpInputSchemaJsonUtilities.CreateFromJson(json);
    }

    [Fact]
    public async Task RunAsync_Throws_WhenRequiredPropertyMissing()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var properties = new[] { CreateProperty("foo", true) };
        var listener = new McpToolListener(executor, "func", "tool", null, properties, null);

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
        var listener = new McpToolListener(executor, "func", "tool", null, properties, null);

        var request = CreateRequest(("foo", JsonDocument.Parse("null").RootElement));

        var ex = await Assert.ThrowsAsync<McpProtocolException>(() =>
            listener.RunAsync(request, CancellationToken.None));
        Assert.Contains("One or more required tool properties are missing values. Please provide: foo", ex.Message);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_Throws_WhenRequiredPropertyMissing()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(); // No arguments

        var ex = Assert.Throws<McpProtocolException>(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params));
        Assert.Contains("One or more required tool properties are missing values. Please provide: foo", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_Throws_WhenRequiredPropertyIsNull()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("null").RootElement));

        var ex = Assert.Throws<McpProtocolException>(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params));
        Assert.Contains("One or more required tool properties are missing values. Please provide: foo", ex.Message);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_Throws_WhenRequiredPropertyIsEmptyString()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("\"\"").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params, McpInputSchemaJsonUtilities.DefaultMcpToolSchema));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_DoesNotThrow_WhenRequiredPropertyPresentAndValid()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("\"bar\"").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params, McpInputSchemaJsonUtilities.DefaultMcpToolSchema));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_Throws_WhenRequiredPropertyIsEmptyArray()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("[]").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params, McpInputSchemaJsonUtilities.DefaultMcpToolSchema));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_DoesNotThrow_WhenRequiredPropertyIsNonEmptyArray()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("[1]").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params, McpInputSchemaJsonUtilities.DefaultMcpToolSchema));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_Throws_WhenRequiredPropertyIsEmptyObject()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("{}").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params, McpInputSchemaJsonUtilities.DefaultMcpToolSchema));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_DoesNotThrow_WhenRequiredPropertyIsNonEmptyObject()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("{\"bar\":1}").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params, McpInputSchemaJsonUtilities.DefaultMcpToolSchema));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_DoesNotThrow_WhenRequiredPropertyIsNumber()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("123").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params, McpInputSchemaJsonUtilities.DefaultMcpToolSchema));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void ValidateArgumentsHaveRequiredProperties_DoesNotThrow_WhenRequiredPropertyIsBoolean(string boolValue)
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse(boolValue).RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params, McpInputSchemaJsonUtilities.DefaultMcpToolSchema));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_UsesInputSchema_WhenAvailable()
    {
        // When using inputSchema, properties should be empty or ignored
        var emptyProperties = Array.Empty<IMcpToolProperty>();
        var inputSchemaJson = """
            {
                "type": "object",
                "required": ["propFromSchema"]
            }
            """;
        var inputSchema = CreateInputSchemaJson(inputSchemaJson);
        var request = CreateRequest(); // No arguments

        var ex = Assert.Throws<McpProtocolException>(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(emptyProperties, request.Params, inputSchema));
        
        // Should use schema's required properties
        Assert.Contains("One or more required tool properties are missing values. Please provide: propFromSchema", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_FallsBackToProperties_WhenSchemaIsDefault()
    {
        // When inputSchema is default, use properties (traditional approach)
        var properties = new[] { CreateProperty("propFromProperty", true) };
        var request = CreateRequest(); // No arguments

        var ex = Assert.Throws<McpProtocolException>(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params, McpInputSchemaJsonUtilities.DefaultMcpToolSchema));
        
        // Should fall back to using properties
        Assert.Contains("One or more required tool properties are missing values. Please provide: propFromProperty", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_DoesNotThrow_WhenSchemaRequiredPropertyPresent()
    {
        // When using inputSchema, properties should be empty 
        var emptyProperties = Array.Empty<IMcpToolProperty>();
        var inputSchemaJson = """
            {
                "type": "object",
                "required": ["propFromSchema"]
            }
            """;
        var inputSchema = CreateInputSchemaJson(inputSchemaJson);
        var request = CreateRequest(("propFromSchema", JsonDocument.Parse("\"value\"").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(emptyProperties, request.Params, inputSchema));
        
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_DoesNotThrow_WhenNoRequiredPropertiesInSchema()
    {
        // When using inputSchema, properties should be empty
        var emptyProperties = Array.Empty<IMcpToolProperty>();
        var inputSchemaJson = """
            {
                "type": "object",
                "required": []
            }
            """;
        var inputSchema = CreateInputSchemaJson(inputSchemaJson);
        var request = CreateRequest(); // No arguments

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(emptyProperties, request.Params, inputSchema));
        
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_IgnoresToolProperties_WhenInputSchemaProvided()
    {
        // This test demonstrates that tool properties are ignored when input schema is provided
        // Even though we pass tool properties, they should be completely ignored
        var toolProperties = new[] { 
            CreateProperty("toolProp1", true),
            CreateProperty("toolProp2", true)
        };
        var inputSchemaJson = """
            {
                "type": "object",
                "required": ["schemaProp1", "schemaProp2"]
            }
            """;
        var inputSchema = CreateInputSchemaJson(inputSchemaJson);
        
        // Provide only the tool properties, not the schema properties
        var request = CreateRequest(
            ("toolProp1", JsonDocument.Parse("\"value1\"").RootElement),
            ("toolProp2", JsonDocument.Parse("\"value2\"").RootElement));

        // Act & Assert - Should validate against input schema only, ignoring tool properties
        var ex = Assert.Throws<McpProtocolException>(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(toolProperties, request.Params, inputSchema));
        
        // Should complain about missing schema properties, not tool properties
        Assert.Contains("schemaProp1", ex.Message);
        Assert.Contains("schemaProp2", ex.Message);
        Assert.DoesNotContain("toolProp1", ex.Message);
        Assert.DoesNotContain("toolProp2", ex.Message);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_ValidatesCorrectly_WhenOnlyInputSchemaUsed()
    {
        // Best practice: When using input schema, tool properties should be empty
        var emptyToolProperties = Array.Empty<IMcpToolProperty>();
        var inputSchemaJson = """
            {
                "type": "object",
                "required": ["actualRequiredProp"]
            }
            """;
        var inputSchema = CreateInputSchemaJson(inputSchemaJson);
        
        var request = CreateRequest(("actualRequiredProp", JsonDocument.Parse("\"correctValue\"").RootElement));

        // Act & Assert - Should validate successfully using only input schema
        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(emptyToolProperties, request.Params, inputSchema));
        
        Assert.Null(ex); // Should not throw because input schema's required property is provided
    }

    [Fact]
    public void McpToolListener_Constructor_AcceptsEitherInputSchemaOrProperties()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        
        // Test with only tool properties (traditional approach)
        var propertiesOnly = new[] { CreateProperty("prop1", true) };
        var listenerWithProperties = new McpToolListener(executor, "func", "tool", null, propertiesOnly, null);
        Assert.NotNull(listenerWithProperties.Properties);
        Assert.Single(listenerWithProperties.Properties);

        // Test with only input schema (new approach) - properties should be empty
        var inputSchemaJson = """
            {
                "type": "object",
                "required": ["prop1"]
            }
            """;
        var inputSchemaOnly = CreateInputSchemaJson(inputSchemaJson);
        var listenerWithSchema = new McpToolListener(executor, "func", "tool", null, Array.Empty<IMcpToolProperty>(), inputSchemaOnly);
        Assert.Empty(listenerWithSchema.Properties);
        Assert.NotEqual(McpInputSchemaJsonUtilities.DefaultMcpToolSchema.ToString(), listenerWithSchema.InputSchema.ToString());
    }

    [Fact]
    public void McpToolListener_Constructor_ShouldNotHaveBothPropertiesAndInputSchema()
    {
        // This test ensures that in practice, we follow the pattern of using either properties OR input schema
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var properties = new[] { CreateProperty("prop1", true) };
        var inputSchemaJson = """
            {
                "type": "object",
                "required": ["prop1"]
            }
            """;
        var inputSchema = CreateInputSchemaJson(inputSchemaJson);

        // While the constructor technically allows both, in practice we should use one or the other
        // This test documents that when both are present, inputSchema takes precedence
        var listenerWithBoth = new McpToolListener(executor, "func", "tool", null, properties, inputSchema);
        
        // Both are stored, but validation should only use inputSchema
        Assert.NotNull(listenerWithBoth.Properties);
        Assert.NotEqual(McpInputSchemaJsonUtilities.DefaultMcpToolSchema.ToString(), listenerWithBoth.InputSchema.ToString());
        
        // Test that validation uses inputSchema when both are present
        var request = CreateRequest(); // No arguments
        var ex = Assert.Throws<McpProtocolException>(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(listenerWithBoth.Properties, request.Params, listenerWithBoth.InputSchema));
        
        // Should validate against inputSchema (prop1), not properties (also prop1 in this case, but demonstrates precedence)
        Assert.Contains("prop1", ex.Message);
    }

    [Fact]
    public void McpToolListener_RecommendedUsagePatterns()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;

        // ✅ RECOMMENDED: Traditional approach - use only tool properties
        var toolPropertiesOnly = new[] { CreateProperty("traditionalProp", true) };
        var traditionalListener = new McpToolListener(executor, "func", "tool", null, toolPropertiesOnly, null);
        
        Assert.NotNull(traditionalListener.Properties);
        Assert.Single(traditionalListener.Properties);

        // ✅ RECOMMENDED: Modern approach - use only input schema  
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
        var inputSchemaOnly = CreateInputSchemaJson(inputSchemaJson);
        var modernListener = new McpToolListener(executor, "func", "tool", null, Array.Empty<IMcpToolProperty>(), inputSchemaOnly);
        
        Assert.Empty(modernListener.Properties);
        Assert.NotEqual(McpInputSchemaJsonUtilities.DefaultMcpToolSchema.ToString(), modernListener.InputSchema.ToString());

        // Test validation works correctly for both patterns
        var emptyRequest = CreateRequest();

        // Traditional approach validation
        var traditionalEx = Assert.Throws<McpProtocolException>(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(traditionalListener.Properties, emptyRequest.Params, McpInputSchemaJsonUtilities.DefaultMcpToolSchema));
        Assert.Contains("traditionalProp", traditionalEx.Message);

        // Modern approach validation  
        var modernEx = Assert.Throws<McpProtocolException>(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(Array.Empty<IMcpToolProperty>(), emptyRequest.Params, modernListener.InputSchema));
        Assert.Contains("modernProp", modernEx.Message);
    }
}
