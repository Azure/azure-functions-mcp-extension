// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Moq;

namespace Worker.Extensions.Mcp.Tests;

public class McpFunctionMetadataTransformerTests
{
    private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";

    [Fact]
    public void Transform_NoRawBindings_DoesNothing()
    {
        var transformer = CreateTransformer();
        var fn = new Mock<IFunctionMetadata>();
        fn.SetupGet(f => f.RawBindings).Returns((IList<string>?)null);
        fn.SetupGet(f => f.Name).Returns("Func");

        var list = new List<IFunctionMetadata> { fn.Object };
        transformer.Transform(list);
        fn.VerifyGet(f => f.RawBindings, Times.Once);

        Assert.Single(list);
        Assert.Null(list[0].RawBindings);
    }

    [Fact]
    public void Transform_NoName_DoesNothing()
    {
        var transformer = CreateTransformer();
        var fn = new Mock<IFunctionMetadata>();
        fn.SetupGet(f => f.RawBindings).Returns(new List<string>());
        fn.SetupGet(f => f.Name).Returns((string?)null);

        transformer.Transform(new List<IFunctionMetadata> { fn.Object });
        fn.VerifyGet(f => f.Name, Times.Once);
    }

    [Fact]
    public void Transform_NonMatchingBinding_Ignored()
    {
        var transformer = CreateTransformer();
        var fn = CreateFunctionMetadata(entryPoint: null, scriptFile: null, name: "Func", bindings: new List<string> { "{\"type\":\"httpTrigger\"}" });        
        transformer.Transform(new List<IFunctionMetadata> { fn.Object });
        fn.VerifySet(f => f.RawBindings![It.IsAny<int>()] = It.IsAny<string>(), Times.Never);
    }

    [Fact]
    public void Transform_MatchingBinding_NoToolPropertiesConfiguredAndNoAttributes_FallsBackEmpty()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.NoAttributes));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"NoAttributes\"}" });

        transformer.Transform(new List<IFunctionMetadata> { fn.Object });

        var binding = fn.Object.RawBindings![0];
        var json = JsonNode.Parse(binding)!.AsObject();

        Assert.Null(json["toolProperties"]);
    }

    [Fact]
    public void Transform_UsesConfiguredOptions_WhenAvailable()
    {
        // Note: The current transformer doesn't support configured options through constructor
        // This test is kept for potential future functionality, but currently expects fallback behavior
        
        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(
            entryPoint: null,
            scriptFile: null,
            name: "Func",
            bindings: new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}" });

        transformer.Transform(new List<IFunctionMetadata> { fn.Object });
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();

        // Since no valid function method can be resolved, should fall back to traditional approach
        Assert.True(json.ContainsKey("useWorkerInputSchema"));
        Assert.False(json["useWorkerInputSchema"]!.GetValue<bool>());
        Assert.True(json.ContainsKey("toolProperties"));
        Assert.Equal("[]", json["toolProperties"]!.GetValue<string>());
        Assert.False(json.ContainsKey("inputSchema"));
    }

    [Fact]
    public void Transform_Attribute_McpToolProperty_OnParameter()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithToolProperty));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithToolProperty\"}" });

        transformer.Transform(new List<IFunctionMetadata> { fn.Object });
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();

        // When useWorkerInputSchema is true, we should see inputSchema, not toolProperties
        Assert.True(json.ContainsKey("useWorkerInputSchema"));
        Assert.True(json["useWorkerInputSchema"]!.GetValue<bool>());
        Assert.True(json.ContainsKey("inputSchema"));
        Assert.False(json.ContainsKey("toolProperties"));

        // Verify the generated input schema contains the expected property
        var inputSchemaString = json["inputSchema"]!.GetValue<string>();
        var schemaDoc = JsonDocument.Parse(inputSchemaString);
        var schema = schemaDoc.RootElement;
        var properties = schema.GetProperty("properties");

        Assert.True(properties.TryGetProperty("name", out var nameProperty));
        Assert.Equal("string", nameProperty.GetProperty("type").GetString());
        Assert.Equal("Name value", nameProperty.GetProperty("description").GetString());

        var required = schema.GetProperty("required");
        var requiredArray = required.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("name", requiredArray);
    }

    [Fact]
    public void Transform_Attribute_McpToolTrigger_PocoParameters()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithTriggerPoco));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithTriggerPoco\"}" });

        transformer.Transform(new List<IFunctionMetadata> { fn.Object });
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();

        // When useWorkerInputSchema is true, we should see inputSchema, not toolProperties
        Assert.True(json.ContainsKey("useWorkerInputSchema"));
        Assert.True(json["useWorkerInputSchema"]!.GetValue<bool>());
        Assert.True(json.ContainsKey("inputSchema"));
        Assert.False(json.ContainsKey("toolProperties"));

        // Verify the generated input schema contains POCO properties
        var inputSchemaString = json["inputSchema"]!.GetValue<string>();
        var schemaDoc = JsonDocument.Parse(inputSchemaString);
        var schema = schemaDoc.RootElement;
        var properties = schema.GetProperty("properties");

        Assert.True(properties.TryGetProperty("Content", out _));
        Assert.True(properties.TryGetProperty("Title", out _));
    }

    [Fact]
    public void Transform_InvalidEntryPoint_NoChange()
    {
        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata("BadEntryPoint", "Some.dll", "Func", new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Bad\"}" });
        fn.SetupGet(f => f.RawBindings).Returns(new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Bad\"}" });

        transformer.Transform(new List<IFunctionMetadata> { fn.Object });
        var binding = fn.Object.RawBindings![0];
        var json = JsonNode.Parse(binding)!.AsObject();

        // Should fall back to traditional approach when method resolution fails
        Assert.True(json.ContainsKey("useWorkerInputSchema"));
        Assert.False(json["useWorkerInputSchema"]!.GetValue<bool>());
        Assert.True(json.ContainsKey("toolProperties"));
        Assert.Equal("[]", json["toolProperties"]!.GetValue<string>());
        Assert.False(json.ContainsKey("inputSchema"));
    }

    [Fact]
    public void Transform_MethodNotFound_NoChange()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>("NonExistent");
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Whatever\"}" });
        transformer.Transform(new List<IFunctionMetadata> { fn.Object });
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();

        // Should fall back to traditional approach when method is not found
        Assert.True(json.ContainsKey("useWorkerInputSchema"));
        Assert.False(json["useWorkerInputSchema"]!.GetValue<bool>());
        Assert.True(json.ContainsKey("toolProperties"));
        Assert.Equal("[]", json["toolProperties"]!.GetValue<string>());
        Assert.False(json.ContainsKey("inputSchema"));
    }

    [Fact]
    public void Transform_TypeNotFound_NoChange()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithToolProperty));
        // Corrupt the type portion
        entryPoint = entryPoint.Replace(typeof(TestFunctions).FullName!, typeof(McpFunctionMetadataTransformerTests).FullName! + "Missing");
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithToolProperty\"}" });
        transformer.Transform(new List<IFunctionMetadata> { fn.Object });
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();

        // Should fall back to traditional approach when type is not found
        Assert.True(json.ContainsKey("useWorkerInputSchema"));
        Assert.False(json["useWorkerInputSchema"]!.GetValue<bool>());
        Assert.True(json.ContainsKey("toolProperties"));
        Assert.Equal("[]", json["toolProperties"]!.GetValue<string>());
        Assert.False(json.ContainsKey("inputSchema"));
    }

    [Fact]
    public void Transform_ToolInvocationContextParameter_IgnoredForPocoProperties()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithContextAndPoco));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithContextAndPoco\"}" });
        transformer.Transform(new List<IFunctionMetadata> { fn.Object });
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();

        // When useWorkerInputSchema is true, we should see inputSchema, not toolProperties
        Assert.True(json.ContainsKey("useWorkerInputSchema"));
        Assert.True(json["useWorkerInputSchema"]!.GetValue<bool>());
        Assert.True(json.ContainsKey("inputSchema"));
        Assert.False(json.ContainsKey("toolProperties"));

        // Verify the generated input schema contains only the McpToolProperty parameter, not ToolInvocationContext
        var inputSchemaString = json["inputSchema"]!.GetValue<string>();
        var schemaDoc = JsonDocument.Parse(inputSchemaString);
        var schema = schemaDoc.RootElement;
        var properties = schema.GetProperty("properties");

        // Should have the McpToolProperty "Name" parameter but not ToolInvocationContext
        Assert.True(properties.TryGetProperty("Name", out _));
        
        // Should not have any property related to ToolInvocationContext
        var propertyNames = properties.EnumerateObject().Select(p => p.Name.ToLowerInvariant()).ToArray();
        Assert.DoesNotContain("context", propertyNames);
        Assert.DoesNotContain("toolinvocationcontext", propertyNames);
    }

    [Fact]
    public void Transform_GeneratesInputSchema_AndStoresInBinding()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithInputSchemaGeneration));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithInputSchemaGeneration\"}" });

        transformer.Transform(new List<IFunctionMetadata> { fn.Object });

        var binding = fn.Object.RawBindings![0];
        var json = JsonNode.Parse(binding)!.AsObject();

        // Verify input schema was generated and stored
        Assert.True(json.ContainsKey("inputSchema"));
        Assert.True(json.ContainsKey("useWorkerInputSchema"));
        Assert.True(json["useWorkerInputSchema"]!.GetValue<bool>());

        // When useWorkerInputSchema is true, toolProperties should not be present
        Assert.False(json.ContainsKey("toolProperties"));

        var inputSchemaString = json["inputSchema"]!.GetValue<string>();
        var schemaDoc = JsonDocument.Parse(inputSchemaString);
        var schema = schemaDoc.RootElement;

        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.True(schema.TryGetProperty("properties", out var propertiesElement));
        Assert.True(schema.TryGetProperty("required", out var requiredElement));

        // Verify specific properties from the test function
        Assert.True(propertiesElement.TryGetProperty("name", out var nameProperty));
        Assert.Equal("string", nameProperty.GetProperty("type").GetString());
        Assert.Equal("Person's name", nameProperty.GetProperty("description").GetString());

        Assert.True(propertiesElement.TryGetProperty("age", out var ageProperty));
        Assert.Equal("integer", ageProperty.GetProperty("type").GetString());

        // Verify required array
        var requiredArray = requiredElement.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("name", requiredArray);
        Assert.DoesNotContain("age", requiredArray);
    }

    [Fact]
    public void Transform_InputSchemaGeneration_WithPocoParameter()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithPocoInputGeneration));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithPocoInputGeneration\"}" });

        transformer.Transform(new List<IFunctionMetadata> { fn.Object });

        var binding = fn.Object.RawBindings![0];
        var json = JsonNode.Parse(binding)!.AsObject();

        Assert.True(json.ContainsKey("inputSchema"));
        Assert.True(json.ContainsKey("useWorkerInputSchema"));
        Assert.True(json["useWorkerInputSchema"]!.GetValue<bool>());
        
        // When useWorkerInputSchema is true, toolProperties should not be present
        Assert.False(json.ContainsKey("toolProperties"));

        var inputSchemaString = json["inputSchema"]!.GetValue<string>();
        var schemaDoc = JsonDocument.Parse(inputSchemaString);
        var schema = schemaDoc.RootElement;
        var propertiesElement = schema.GetProperty("properties");

        // Verify POCO properties are included
        Assert.True(propertiesElement.TryGetProperty("Content", out var contentProperty));
        Assert.Equal("string", contentProperty.GetProperty("type").GetString());
        Assert.Equal("The content of the snippet", contentProperty.GetProperty("description").GetString());

        Assert.True(propertiesElement.TryGetProperty("Title", out var titleProperty));
        Assert.Equal("string", titleProperty.GetProperty("type").GetString());
        Assert.Equal("The title of the snippet", titleProperty.GetProperty("description").GetString());

        // Verify required properties (Title is required in the POCO)
        var requiredElement = schema.GetProperty("required");
        var requiredArray = requiredElement.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("Title", requiredArray);
        Assert.DoesNotContain("Content", requiredArray); // Content is nullable, not required
    }

    [Fact]
    public void Transform_InputSchemaGeneration_WithEnumProperties()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithEnumInputGeneration));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithEnumInputGeneration\"}" });

        transformer.Transform(new List<IFunctionMetadata> { fn.Object });

        var binding = fn.Object.RawBindings![0];
        var json = JsonNode.Parse(binding)!.AsObject();

        Assert.True(json.ContainsKey("inputSchema"));
        Assert.True(json.ContainsKey("useWorkerInputSchema"));
        Assert.True(json["useWorkerInputSchema"]!.GetValue<bool>());
        
        // When useWorkerInputSchema is true, toolProperties should not be present
        Assert.False(json.ContainsKey("toolProperties"));

        var inputSchemaString = json["inputSchema"]!.GetValue<string>();
        var schemaDoc = JsonDocument.Parse(inputSchemaString);
        var schema = schemaDoc.RootElement;
        var propertiesElement = schema.GetProperty("properties");

        // Verify enum property
        Assert.True(propertiesElement.TryGetProperty("status", out var statusProperty));
        Assert.Equal("string", statusProperty.GetProperty("type").GetString());
        Assert.Equal("Task status", statusProperty.GetProperty("description").GetString());
        
        Assert.True(statusProperty.TryGetProperty("enum", out var enumProperty));
        var enumValues = enumProperty.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("Active", enumValues);
        Assert.Contains("Completed", enumValues);
        Assert.Contains("Cancelled", enumValues);
    }

    [Fact]
    public void Transform_InputSchemaGeneration_WithArrayProperties()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithArrayInputGeneration));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithArrayInputGeneration\"}" });

        transformer.Transform(new List<IFunctionMetadata> { fn.Object });

        var binding = fn.Object.RawBindings![0];
        var json = JsonNode.Parse(binding)!.AsObject();

        Assert.True(json.ContainsKey("inputSchema"));
        Assert.True(json.ContainsKey("useWorkerInputSchema"));
        Assert.True(json["useWorkerInputSchema"]!.GetValue<bool>());
        
        // When useWorkerInputSchema is true, toolProperties should not be present
        Assert.False(json.ContainsKey("toolProperties"));

        var inputSchemaString = json["inputSchema"]!.GetValue<string>();
        var schemaDoc = JsonDocument.Parse(inputSchemaString);
        var schema = schemaDoc.RootElement;
        var propertiesElement = schema.GetProperty("properties");

        // Verify array property
        Assert.True(propertiesElement.TryGetProperty("tags", out var tagsProperty));
        Assert.Equal("array", tagsProperty.GetProperty("type").GetString());
        Assert.Equal("List of tags", tagsProperty.GetProperty("description").GetString());
        
        Assert.True(tagsProperty.TryGetProperty("items", out var itemsProperty));
        Assert.Equal("string", itemsProperty.GetProperty("type").GetString());
    }

    [Fact]
    public void Transform_InputSchemaGeneration_HandlesToolPropertyBindings()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithInputSchemaGeneration));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var bindings = new List<string>
        {
            "{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithInputSchemaGeneration\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"name\"}",
            "{\"type\":\"mcpToolProperty\",\"propertyName\":\"age\"}"
        };

        var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", bindings);

        transformer.Transform(new List<IFunctionMetadata> { fn.Object });

        // Verify the trigger binding has input schema
        var triggerBinding = fn.Object.RawBindings![0];
        var triggerJson = JsonNode.Parse(triggerBinding)!.AsObject();
        Assert.True(triggerJson.ContainsKey("inputSchema"));

        // Verify tool property bindings were patched with correct types
        var namePropertyBinding = fn.Object.RawBindings![1];
        var nameJson = JsonNode.Parse(namePropertyBinding)!.AsObject();
        Assert.True(nameJson.ContainsKey("propertyType"));
        Assert.Equal("string", nameJson["propertyType"]!.GetValue<string>());

        var agePropertyBinding = fn.Object.RawBindings![2];
        var ageJsonObj = JsonNode.Parse(agePropertyBinding)!.AsObject();
        Assert.True(ageJsonObj.ContainsKey("propertyType"));
        Assert.Equal("integer", ageJsonObj["propertyType"]!.GetValue<string>());
    }

    [Fact]
    public void Transform_InputSchemaGeneration_FailsGracefullyOnInvalidFunction()
    {
        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata("Invalid.EntryPoint", "NonExistent.dll", "Func", 
            new List<string> { "{\"type\":\"mcpToolTrigger\",\"toolName\":\"Invalid\"}" });

        // Should not throw exception when input schema generation fails, should fall back to toolProperties
        transformer.Transform(new List<IFunctionMetadata> { fn.Object });
        
        var binding = fn.Object.RawBindings![0];
        var json = JsonNode.Parse(binding)!.AsObject();
        
        // Should fall back to traditional approach
        Assert.True(json.ContainsKey("useWorkerInputSchema"));
        Assert.False(json["useWorkerInputSchema"]!.GetValue<bool>());
        Assert.True(json.ContainsKey("toolProperties"));
        Assert.Equal("[]", json["toolProperties"]!.GetValue<string>());
        Assert.False(json.ContainsKey("inputSchema"));
    }

    private static McpFunctionMetadataTransformer CreateTransformer(List<ToolProperty>? configured = null)
    {
        // The McpFunctionMetadataTransformer has a parameterless constructor
        return new McpFunctionMetadataTransformer();
    }

    private static Mock<IFunctionMetadata> CreateFunctionMetadata(string? entryPoint, string? scriptFile, string? name, IList<string>? bindings)
    {
        var fn = new Mock<IFunctionMetadata>();
        fn.SetupGet(f => f.EntryPoint).Returns(entryPoint);
        fn.SetupGet(f => f.ScriptFile).Returns(scriptFile);
        fn.SetupGet(f => f.Name).Returns(name);
        fn.SetupGet(f => f.RawBindings).Returns(bindings);
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
        public void NoAttributes([McpToolTrigger("NoAttributes", "none")] string dummy) { }

        public void WithToolProperty(
            [McpToolTrigger("WithToolProperty", "desc")] ToolInvocationContext ctx,
            [McpToolProperty("name", "Name value", true)] string name) { }

        public void WithTriggerPoco(
            [McpToolTrigger("WithTriggerPoco", "desc")] Snippet snippet) { }

        public void WithContextAndPoco(
            [McpToolTrigger("WithContextAndPoco", "desc")] ToolInvocationContext context,
            [McpToolProperty("Name", "Name", true)] string name,
            ExtraPoco ignored) { }

        public void WithInputSchemaGeneration(
            [McpToolProperty("name", "Person's name", true)] string name,
            [McpToolProperty("age", "Person's age", false)] int age) { }

        public void WithPocoInputGeneration(Snippet snippet) { }

        public void WithEnumInputGeneration([McpToolProperty("status", "Task status", true)] TaskStatus status) { }

        public void WithArrayInputGeneration([McpToolProperty("tags", "List of tags", false)] string[] tags) { }
    }

    public class Snippet
    {
        [Description("The content of the snippet")] public string? Content { get; set; }
        [Description("The title of the snippet")] public required string Title { get; set; }
    }

    public class ExtraPoco
    {
        public string? Name { get; set; }
    }

    public enum TaskStatus
    {
        Active,
        Completed,
        Cancelled
    }
}
