// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Trigger;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpToolExtensionsTests
{
    private static IMcpTool CreateTool(params IMcpToolProperty[] properties)
    {
        var mock = new Mock<IMcpTool>();
        mock.SetupGet(t => t.Properties).Returns(properties);
        mock.SetupGet(t => t.Name).Returns("tool");
        mock.SetupProperty(t => t.Description, "desc");
        mock.Setup(t => t.RunAsync(It.IsAny<RequestContext<CallToolRequestParams>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CallToolResult());
        return mock.Object;
    }

    private static IMcpTool CreateToolWithInputSchema(McpInputSchema inputSchema, params IMcpToolProperty[] properties)
    {
        var mock = new Mock<IMcpTool>();
        mock.SetupGet(t => t.Properties).Returns(properties);
        mock.SetupGet(t => t.InputSchema).Returns(inputSchema);
        mock.SetupGet(t => t.Name).Returns("tool");
        mock.SetupProperty(t => t.Description, "desc");
        mock.Setup(t => t.RunAsync(It.IsAny<RequestContext<CallToolRequestParams>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CallToolResult());
        return mock.Object;
    }

    private static IMcpToolProperty CreateProperty(string name, string type, string? description = null, bool required = false, bool isArray = false)
    {
        var mock = new Mock<IMcpToolProperty>();
        mock.SetupAllProperties();
        mock.Object.PropertyName = name;
        mock.Object.PropertyType = type;
        mock.Object.Description = description;
        mock.Object.IsRequired = required;
        mock.Object.IsArray = isArray;
        return mock.Object;
    }

    [Fact]
    public void GetPropertiesInputSchema_NoProperties_ReturnsEmptySchema()
    {
        var tool = CreateTool();

        var schema = tool.GetPropertiesInputSchema();

        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.Empty(schema.GetProperty("properties").EnumerateObject());
        Assert.Equal(0, schema.GetProperty("required").GetArrayLength());
    }

    [Fact]
    public void GetPropertiesInputSchema_IncludesAllPropertiesAndRequiredArray()
    {
        var prop1 = CreateProperty("first", "string", "First property", required: true);
        var prop2 = CreateProperty("second", "number", null, required: false);
        var tool = CreateTool(prop1, prop2);

        var schema = tool.GetPropertiesInputSchema();

        Assert.Equal("object", schema.GetProperty("type").GetString());
        var properties = schema.GetProperty("properties");

        var first = properties.GetProperty("first");
        Assert.Equal("string", first.GetProperty("type").GetString());
        Assert.Equal("First property", first.GetProperty("description").GetString());

        var second = properties.GetProperty("second");
        Assert.Equal("number", second.GetProperty("type").GetString());
        // Null descriptions become empty string
        Assert.Equal(string.Empty, second.GetProperty("description").GetString());

        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(required);
        Assert.Contains("first", required);
    }

    [Fact]
    public void GetPropertiesInputSchema_ArrayProperty_SerializesWithArrayTypeAndItems()
    {
        var arrayProp = CreateProperty("tags", "string", "Tags list", required: true, isArray: true);
        var scalarProp = CreateProperty("count", "number", null, required: false, isArray: false);
        var tool = CreateTool(arrayProp, scalarProp);

        var schema = tool.GetPropertiesInputSchema();

        var properties = schema.GetProperty("properties");
        var tags = properties.GetProperty("tags");
        Assert.Equal("array", tags.GetProperty("type").GetString());

        var items = tags.GetProperty("items");
        Assert.Equal("string", items.GetProperty("type").GetString());
        Assert.Equal("Tags list", tags.GetProperty("description").GetString());

        var count = properties.GetProperty("count");
        Assert.Equal("number", count.GetProperty("type").GetString());
        Assert.False(count.TryGetProperty("items", out var _));

        Assert.Equal(string.Empty, count.GetProperty("description").GetString());

        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(required);
        Assert.Contains("tags", required);
    }

    [Fact]
    public void GetPropertiesInputSchema_UsesInputSchema_WhenPopulated()
    {
        // Arrange - Create an input schema with specific properties
        var inputSchema = new McpInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpPropertySchema>
            {
                ["schemaProperty"] = new McpPropertySchema 
                { 
                    Type = "string", 
                    Description = "Property from schema" 
                },
                ["anotherSchemaProperty"] = new McpPropertySchema 
                { 
                    Type = "number", 
                    Description = "Another property from schema" 
                }
            },
            Required = new[] { "schemaProperty" }
        };

        // Create tool properties that should be ignored when input schema is present
        var toolProperty = CreateProperty("toolProperty", "boolean", "Property from tool", required: true);
        var tool = CreateToolWithInputSchema(inputSchema, toolProperty);

        // Act
        var schema = tool.GetPropertiesInputSchema();

        // Assert - Should use input schema, not tool properties
        Assert.Equal("object", schema.GetProperty("type").GetString());
        
        var properties = schema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("schemaProperty", out var schemaProp));
        Assert.Equal("string", schemaProp.GetProperty("type").GetString());
        Assert.Equal("Property from schema", schemaProp.GetProperty("description").GetString());

        Assert.True(properties.TryGetProperty("anotherSchemaProperty", out var anotherSchemaProp));
        Assert.Equal("number", anotherSchemaProp.GetProperty("type").GetString());
        Assert.Equal("Another property from schema", anotherSchemaProp.GetProperty("description").GetString());

        // Should NOT contain tool properties
        Assert.False(properties.TryGetProperty("toolProperty", out var _));

        // Required array should come from input schema
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(required);
        Assert.Contains("schemaProperty", required);
        Assert.DoesNotContain("toolProperty", required);
    }

    [Fact]
    public void GetPropertiesInputSchema_UsesInputSchema_WithArrayProperties()
    {
        // Arrange - Create an input schema with array properties
        var inputSchema = new McpInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpPropertySchema>
            {
                ["arrayProp"] = new McpPropertySchema 
                { 
                    Type = "array",
                    Description = "Array property from schema",
                    Items = new McpPropertySchema { Type = "string" }
                },
                ["scalarProp"] = new McpPropertySchema 
                { 
                    Type = "boolean", 
                    Description = "Scalar property from schema" 
                }
            },
            Required = new[] { "arrayProp", "scalarProp" }
        };

        var tool = CreateToolWithInputSchema(inputSchema);

        // Act
        var schema = tool.GetPropertiesInputSchema();

        // Assert
        var properties = schema.GetProperty("properties");
        
        Assert.True(properties.TryGetProperty("arrayProp", out var arrayProp));
        Assert.Equal("array", arrayProp.GetProperty("type").GetString());
        Assert.Equal("Array property from schema", arrayProp.GetProperty("description").GetString());
        Assert.True(arrayProp.TryGetProperty("items", out var items));
        Assert.Equal("string", items.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("scalarProp", out var scalarProp));
        Assert.Equal("boolean", scalarProp.GetProperty("type").GetString());
        Assert.Equal("Scalar property from schema", scalarProp.GetProperty("description").GetString());

        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(2, required.Length);
        Assert.Contains("arrayProp", required);
        Assert.Contains("scalarProp", required);
    }

    [Fact]
    public void GetPropertiesInputSchema_UsesInputSchema_WithEmptyRequired()
    {
        // Arrange - Create an input schema with no required properties
        var inputSchema = new McpInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpPropertySchema>
            {
                ["optionalProp"] = new McpPropertySchema 
                { 
                    Type = "string", 
                    Description = "Optional property" 
                }
            },
            Required = Array.Empty<string>()
        };

        // Create a tool property that would be required if input schema wasn't present
        var requiredToolProperty = CreateProperty("requiredToolProp", "number", "Required tool property", required: true);
        var tool = CreateToolWithInputSchema(inputSchema, requiredToolProperty);

        // Act
        var schema = tool.GetPropertiesInputSchema();

        // Assert - Should use input schema with empty required array
        var properties = schema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("optionalProp", out var _));
        Assert.False(properties.TryGetProperty("requiredToolProp", out var _));

        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Empty(required);
    }

    [Fact]
    public void GetPropertiesInputSchema_FallsBackToProperties_WhenInputSchemaIsNull()
    {
        // Arrange - Create tool with null input schema but with tool properties
        var toolProperty = CreateProperty("toolProp", "string", "Tool property", required: true);
        var tool = CreateToolWithInputSchema(null, toolProperty);

        // Act
        var schema = tool.GetPropertiesInputSchema();

        // Assert - Should fall back to tool properties
        var properties = schema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("toolProp", out var prop));
        Assert.Equal("string", prop.GetProperty("type").GetString());
        Assert.Equal("Tool property", prop.GetProperty("description").GetString());

        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(required);
        Assert.Contains("toolProp", required);
    }
}
