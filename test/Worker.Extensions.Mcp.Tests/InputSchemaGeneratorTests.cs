// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Moq;

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

        // Should not include ToolInvocationContext parameter
        Assert.False(properties.TryGetProperty("context", out _));
        
        // Should include the string parameter
        Assert.True(properties.TryGetProperty("name", out var nameProperty));
        Assert.Equal("string", nameProperty.GetProperty("type").GetString());
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
    public void GenerateFromParameters_PocoParameter_GeneratesCorrectSchema()
    {
        var method = typeof(TestFunctions).GetMethod(nameof(TestFunctions.WithPocoParameter))!;
        var parameters = method.GetParameters();

        var schema = InputSchemaGenerator.GenerateFromParameters(parameters);

        var schemaDoc = JsonDocument.Parse(schema.ToJsonString());
        var root = schemaDoc.RootElement;
        var properties = root.GetProperty("properties");

        // Should have properties from the POCO
        Assert.True(properties.TryGetProperty("Name", out var nameProperty));
        Assert.Equal("string", nameProperty.GetProperty("type").GetString());
        Assert.Equal("The person's name", nameProperty.GetProperty("description").GetString());

        Assert.True(properties.TryGetProperty("Age", out var ageProperty));
        Assert.Equal("integer", ageProperty.GetProperty("type").GetString());
        Assert.Equal("The person's age", ageProperty.GetProperty("description").GetString());

        Assert.True(properties.TryGetProperty("Email", out var emailProperty));
        Assert.Equal("string", emailProperty.GetProperty("type").GetString());
        Assert.Equal("Email address", emailProperty.GetProperty("description").GetString());

        // Required properties
        var required = root.GetProperty("required");
        var requiredArray = required.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(2, requiredArray.Length);
        Assert.Contains("Name", requiredArray);
        Assert.Contains("Email", requiredArray);
        Assert.DoesNotContain("Age", requiredArray); // Age is not required
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

        // McpToolProperty parameter
        Assert.True(properties.TryGetProperty("filter", out var filterProperty));
        Assert.Equal("string", filterProperty.GetProperty("type").GetString());
        Assert.Equal("Search filter", filterProperty.GetProperty("description").GetString());

        // POCO properties
        Assert.True(properties.TryGetProperty("Name", out var nameProperty));
        Assert.Equal("string", nameProperty.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("Age", out var ageProperty));
        Assert.Equal("integer", ageProperty.GetProperty("type").GetString());

        // Required properties should include required McpToolProperty and required POCO properties
        var required = root.GetProperty("required");
        var requiredArray = required.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("filter", requiredArray); // Required McpToolProperty
        Assert.Contains("Name", requiredArray); // Required POCO property
        Assert.DoesNotContain("Age", requiredArray); // Not required
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
    }

    public class TestPoco
    {
        [Description("The person's name")]
        [Required]
        public string Name { get; set; } = "";

        [Description("The person's age")]
        public int Age { get; set; }

        [Description("Email address")]
        [Required]
        public string Email { get; set; } = "";
    }

    public enum TaskStatus
    {
        Active,
        Completed,
        Cancelled
    }
}
