// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Moq;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Worker.Extensions.Mcp.Tests;

public class InputSchemaGeneratorTests
{
    private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";

    [Fact]
    public void TryGenerateFromFunction_InvalidEntryPoint_ReturnsFalse()
    {
        var functionMetadata = CreateFunctionMetadata("InvalidEntryPoint", "Test.dll", "TestMethod");

        var result = InputSchemaGenerator.TryGenerateFromFunction(functionMetadata.Object, out var inputSchema);

        Assert.False(result);
        Assert.Null(inputSchema);
    }

    [Fact]
    public void TryGenerateFromFunction_ValidMethod_ReturnsTrue()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.SimpleMethod));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var functionMetadata = CreateFunctionMetadata(entryPoint, scriptFile, "TestMethod");

        var result = InputSchemaGenerator.TryGenerateFromFunction(functionMetadata.Object, out var inputSchema);

        Assert.True(result);
        Assert.NotNull(inputSchema);

        var schema = JsonDocument.Parse(inputSchema.ToJsonString());
        Assert.Equal("object", schema.RootElement.GetProperty("type").GetString());
        Assert.True(schema.RootElement.TryGetProperty("properties", out _));
        Assert.True(schema.RootElement.TryGetProperty("required", out _));
    }

    [Fact]
    public void GenerateFromParameters_NoParameters_ReturnsEmptySchema()
    {
        var parameters = Array.Empty<ParameterInfo>();

        var schema = InputSchemaGenerator.GenerateFromParameters(parameters);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;

        Assert.Equal("object", root.GetProperty("type").GetString());
        Assert.True(root.TryGetProperty("properties", out var properties));
        Assert.Empty(properties.EnumerateObject());
        Assert.True(root.TryGetProperty("required", out var required));
        Assert.Equal(0, required.GetArrayLength());
    }

    [Fact]
    public void GenerateFromParameters_SkipsToolInvocationContext()
    {
        var method = typeof(TestFunctions).GetMethod(nameof(TestFunctions.WithToolInvocationContext))!;
        var parameters = method.GetParameters();

        var schema = InputSchemaGenerator.GenerateFromParameters(parameters);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;
        var properties = root.GetProperty("properties");

        Assert.False(properties.TryGetProperty("context", out _));
        Assert.False(properties.TryGetProperty("name", out _));
    }

    [Fact]
    public void GenerateFromParameters_McpToolPropertyAttribute_GeneratesCorrectSchema()
    {
        var method = typeof(TestFunctions).GetMethod(nameof(TestFunctions.WithMcpToolProperty))!;
        var parameters = method.GetParameters();

        var schema = InputSchemaGenerator.GenerateFromParameters(parameters);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;
        var properties = root.GetProperty("properties");

        Assert.True(properties.TryGetProperty("username", out var usernameProperty));
        Assert.Equal("string", usernameProperty.GetProperty("type").GetString());
        Assert.Equal("The username to greet", usernameProperty.GetProperty("description").GetString());

        var required = root.GetProperty("required");
        var requiredArray = required.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(requiredArray);
        Assert.Contains("username", requiredArray);
    }

    [Fact]
    public void GenerateFromParameters_PocoParameter_WithoutTriggerAttribute_DoesNotInclude()
    {
        var method = typeof(TestFunctions).GetMethod(nameof(TestFunctions.WithPocoParameter))!;
        var parameters = method.GetParameters();

        var schema = InputSchemaGenerator.GenerateFromParameters(parameters);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;
        var properties = root.GetProperty("properties");

        Assert.True(!properties.EnumerateObject().Any());
    }

    [Fact]
    public void GenerateFromParameters_EnumProperty_GeneratesCorrectSchema()
    {
        var method = typeof(TestFunctions).GetMethod(nameof(TestFunctions.WithEnumParameter))!;
        var parameters = method.GetParameters();

        var schema = InputSchemaGenerator.GenerateFromParameters(parameters);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;
        var properties = root.GetProperty("properties");

        Assert.True(properties.TryGetProperty("status", out var statusProperty));
        Assert.Equal("string", statusProperty.GetProperty("type").GetString());
        Assert.Equal("Task status", statusProperty.GetProperty("description").GetString());

        Assert.True(statusProperty.TryGetProperty("enum", out var enumProperty));
        var enumValues = enumProperty.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("Active", enumValues);
        Assert.Contains("Completed", enumValues);
        Assert.Contains("Cancelled", enumValues);
    }

    [Fact]
    public void GenerateFromParameters_ArrayProperty_GeneratesCorrectSchema()
    {
        var method = typeof(TestFunctions).GetMethod(nameof(TestFunctions.WithArrayParameter))!;
        var parameters = method.GetParameters();

        var schema = InputSchemaGenerator.GenerateFromParameters(parameters);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;
        var properties = root.GetProperty("properties");

        Assert.True(properties.TryGetProperty("tags", out var tagsProperty));
        Assert.Equal("array", tagsProperty.GetProperty("type").GetString());
        Assert.Equal("Array of tags", tagsProperty.GetProperty("description").GetString());

        Assert.True(tagsProperty.TryGetProperty("items", out var itemsProperty));
        Assert.Equal("string", itemsProperty.GetProperty("type").GetString());
    }

    [Fact]
    public void GenerateFromParameters_MixedParameters_GeneratesCorrectSchema()
    {
        var method = typeof(TestFunctions).GetMethod(nameof(TestFunctions.WithMixedParameters))!;
        var parameters = method.GetParameters();

        var schema = InputSchemaGenerator.GenerateFromParameters(parameters);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;
        var properties = root.GetProperty("properties");

        Assert.True(properties.TryGetProperty("filter", out var filterProperty));
        Assert.Equal("string", filterProperty.GetProperty("type").GetString());
        Assert.Equal("Search filter", filterProperty.GetProperty("description").GetString());

        // POCO properties should not exist (no McpToolTrigger attribute on the POCO param)
        Assert.False(properties.TryGetProperty("Name", out _));
        Assert.False(properties.TryGetProperty("Age", out _));

        var required = root.GetProperty("required");
        var requiredArray = required.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("filter", requiredArray);
        Assert.DoesNotContain("Name", requiredArray);
        Assert.DoesNotContain("Age", requiredArray);
    }

    [Fact]
    public void GenerateFromParameters_BooleanProperty_GeneratesCorrectSchema()
    {
        var method = typeof(TestFunctions).GetMethod(nameof(TestFunctions.WithBooleanParameter))!;
        var parameters = method.GetParameters();

        var schema = InputSchemaGenerator.GenerateFromParameters(parameters);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;
        var properties = root.GetProperty("properties");

        Assert.True(properties.TryGetProperty("enabled", out var enabledProperty));
        Assert.Equal("boolean", enabledProperty.GetProperty("type").GetString());
        Assert.Equal("Enable feature", enabledProperty.GetProperty("description").GetString());
    }

    [Fact]
    public void GenerateFromParameters_NumberProperty_GeneratesCorrectSchema()
    {
        var method = typeof(TestFunctions).GetMethod(nameof(TestFunctions.WithNumberParameter))!;
        var parameters = method.GetParameters();

        var schema = InputSchemaGenerator.GenerateFromParameters(parameters);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;
        var properties = root.GetProperty("properties");

        Assert.True(properties.TryGetProperty("price", out var priceProperty));
        Assert.Equal("number", priceProperty.GetProperty("type").GetString());
        Assert.Equal("Item price", priceProperty.GetProperty("description").GetString());
    }

    [Fact]
    public void GenerateFromParameters_PocoParameterWithTriggerAttribute_IncludesPocoProperties()
    {
        var method = typeof(TestFunctions).GetMethod(nameof(TestFunctions.WithPocoInputGeneration))!;
        var parameters = method.GetParameters();

        var schema = InputSchemaGenerator.GenerateFromParameters(parameters);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;
        var properties = root.GetProperty("properties");

        Assert.True(properties.TryGetProperty("Name", out var nameProperty));
        Assert.Equal("string", nameProperty.GetProperty("type").GetString());
        Assert.Equal("The person's name", nameProperty.GetProperty("description").GetString());

        Assert.True(properties.TryGetProperty("Age", out var ageProperty));
        Assert.Equal("integer", ageProperty.GetProperty("type").GetString());
        Assert.Equal("The person's age", ageProperty.GetProperty("description").GetString());

        Assert.True(properties.TryGetProperty("Email", out var emailProperty));
        Assert.Equal("string", emailProperty.GetProperty("type").GetString());
        Assert.Equal("Email address", emailProperty.GetProperty("description").GetString());

        var required = root.GetProperty("required");
        var requiredArray = required.EnumerateArray().Select(e => e.GetString()).ToArray();

        Assert.Contains("Name", requiredArray);
        Assert.Contains("Email", requiredArray);
        Assert.DoesNotContain("Age", requiredArray);
        Assert.Equal(2, requiredArray.Length);
    }

    [Fact]
    public void GenerateFromToolProperties_EmptyList_ReturnsEmptySchema()
    {
        var properties = new List<ToolProperty>();

        var schema = InputSchemaGenerator.GenerateFromToolProperties(properties);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;

        Assert.Equal("object", root.GetProperty("type").GetString());
        Assert.Empty(root.GetProperty("properties").EnumerateObject());
        Assert.Equal(0, root.GetProperty("required").GetArrayLength());
    }

    [Fact]
    public void GenerateFromToolProperties_SingleProperty_GeneratesCorrectSchema()
    {
        var properties = new List<ToolProperty>
        {
            new("city", "string", "The city name", isRequired: true)
        };

        var schema = InputSchemaGenerator.GenerateFromToolProperties(properties);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;

        Assert.Equal("object", root.GetProperty("type").GetString());

        var props = root.GetProperty("properties");
        Assert.True(props.TryGetProperty("city", out var cityProp));
        Assert.Equal("string", cityProp.GetProperty("type").GetString());
        Assert.Equal("The city name", cityProp.GetProperty("description").GetString());

        var required = root.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(required);
        Assert.Contains("city", required);
    }

    [Fact]
    public void GenerateFromToolProperties_MultipleProperties_GeneratesCorrectSchema()
    {
        var properties = new List<ToolProperty>
        {
            new("name", "string", "The name", isRequired: true),
            new("age", "integer", "The age", isRequired: false),
            new("active", "boolean", "Is active", isRequired: true)
        };

        var schema = InputSchemaGenerator.GenerateFromToolProperties(properties);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;
        var props = root.GetProperty("properties");

        Assert.Equal(3, props.EnumerateObject().Count());
        Assert.True(props.TryGetProperty("name", out _));
        Assert.True(props.TryGetProperty("age", out _));
        Assert.True(props.TryGetProperty("active", out _));

        var required = root.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(2, required.Length);
        Assert.Contains("name", required);
        Assert.Contains("active", required);
        Assert.DoesNotContain("age", required);
    }

    [Fact]
    public void GenerateFromToolProperties_ArrayProperty_GeneratesCorrectSchema()
    {
        var properties = new List<ToolProperty>
        {
            new("tags", "string", "List of tags", isRequired: false, isArray: true)
        };

        var schema = InputSchemaGenerator.GenerateFromToolProperties(properties);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;
        var props = root.GetProperty("properties");

        Assert.True(props.TryGetProperty("tags", out var tagsProp));
        Assert.Equal("array", tagsProp.GetProperty("type").GetString());
        Assert.Equal("List of tags", tagsProp.GetProperty("description").GetString());

        var items = tagsProp.GetProperty("items");
        Assert.Equal("string", items.GetProperty("type").GetString());
    }

    [Fact]
    public void GenerateFromToolProperties_EnumProperty_GeneratesCorrectSchema()
    {
        var properties = new List<ToolProperty>
        {
            new("status", "string", "Task status", isRequired: true, enumValues: new[] { "Active", "Completed", "Cancelled" })
        };

        var schema = InputSchemaGenerator.GenerateFromToolProperties(properties);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;
        var props = root.GetProperty("properties");

        Assert.True(props.TryGetProperty("status", out var statusProp));
        Assert.Equal("string", statusProp.GetProperty("type").GetString());

        Assert.True(statusProp.TryGetProperty("enum", out var enumProp));
        var enumValues = enumProp.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("Active", enumValues);
        Assert.Contains("Completed", enumValues);
        Assert.Contains("Cancelled", enumValues);
    }

    private static Mock<IFunctionMetadata> CreateFunctionMetadata(string entryPoint, string scriptFile, string name)
    {
        var fn = new Mock<IFunctionMetadata>();
        fn.SetupGet(f => f.EntryPoint).Returns(entryPoint);
        fn.SetupGet(f => f.ScriptFile).Returns(scriptFile);
        fn.SetupGet(f => f.Name).Returns(name);
        return fn;
    }

    private static (string EntryPoint, string ScriptFile, string OutputDir) GetFunctionMetadataInfo<T>(string methodName)
    {
        var type = typeof(T);
        var entryPoint = $"{type.FullName}.{methodName}";
        var scriptFile = Path.GetFileName(type.Assembly.Location);
        var outputDir = Path.GetDirectoryName(type.Assembly.Location)!;
        return (entryPoint, scriptFile, outputDir);
    }

    internal class TestFunctions
    {
        public void SimpleMethod() { }

        public void WithToolInvocationContext(ToolInvocationContext context, string name) { }

        public void WithMcpToolProperty([McpToolProperty("username", "The username to greet", true)] string username) { }

        public void WithPocoParameter(TestPoco poco) { }

        public void WithEnumParameter([McpToolProperty("status", "Task status", true)] TaskStatus status) { }

        public void WithArrayParameter([McpToolProperty("tags", "Array of tags", false)] string[] tags) { }

        public void WithMixedParameters(
            [McpToolProperty("filter", "Search filter", true)] string filter,
            TestPoco poco) { }

        public void WithBooleanParameter([McpToolProperty("enabled", "Enable feature", false)] bool enabled) { }

        public void WithNumberParameter([McpToolProperty("price", "Item price", false)] decimal price) { }

        public void WithPocoInputGeneration([McpToolTrigger("WithPocoInputGeneration", "desc")] TestPoco poco) { }
    }

    public class TestPoco
    {
        [Description("The person's name")]
        public required string Name { get; set; } = "";

        [Description("The person's age")]
        public int Age { get; set; }

        [Description("Email address")]
        public required string Email { get; set; } = "";
    }

    public enum TaskStatus
    {
        Active,
        Completed,
        Cancelled
    }
}
