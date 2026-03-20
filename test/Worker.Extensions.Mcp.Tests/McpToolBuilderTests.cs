// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Worker.Extensions.Mcp.Tests;

public class McpToolBuilderTests
{
    private static McpToolBuilder CreateBuilder(string toolName, out ServiceCollection services)
    {
        services = new ServiceCollection();
        services.AddOptions();

        var appBuilder = new Mock<IFunctionsWorkerApplicationBuilder>();
        appBuilder.SetupGet(b => b.Services).Returns(services);

        return new McpToolBuilder(appBuilder.Object, toolName);
    }

    [Fact]
    public void WithProperty_AddsToolProperty()
    {
        var toolName = "myTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithProperty("prop1", McpToolPropertyType.String, "desc1", required: true);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Single(options.Properties);
        var p = options.Properties[0];
        Assert.Equal("prop1", p.Name);
        Assert.Equal("string", p.Type);
        Assert.Equal("desc1", p.Description);
        Assert.True(p.IsRequired);
        Assert.False(p.IsArray);
    }

    [Fact]
    public void WithProperty_ArrayType_AddsArrayFlag()
    {
        var toolName = "arrayTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithProperty("numbers", McpToolPropertyType.Number.AsArray(), "numbers array");

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Single(options.Properties);
        Assert.True(options.Properties[0].IsArray);
        Assert.Equal("number", options.Properties[0].Type);
    }

    [Fact]
    public void WithProperty_Chaining_AddsMultipleProperties()
    {
        var toolName = "chainTool";
        var builder = CreateBuilder(toolName, out var services);

        builder
            .WithProperty("id", McpToolPropertyType.Integer, "identifier", required: true)
            .WithProperty("tags", McpToolPropertyType.String.AsArray(), "tags");

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Equal(2, options.Properties.Count);

        var id = options.Properties.First(p => p.Name == "id");
        Assert.Equal("integer", id.Type);
        Assert.True(id.IsRequired);
        Assert.False(id.IsArray);

        var tags = options.Properties.First(p => p.Name == "tags");
        Assert.Equal("string", tags.Type);
        Assert.False(tags.IsRequired);
        Assert.True(tags.IsArray);
    }

    [Fact]
    public void WithProperty_EmptyName_Throws()
    {
        var builder = CreateBuilder("tool", out _);
        var ex = Assert.Throws<ArgumentException>(() =>
            builder.WithProperty(string.Empty, McpToolPropertyType.Boolean, "desc"));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void WithProperty_NullType_Throws()
    {
        var builder = CreateBuilder("tool", out _);
        var type = null as McpToolPropertyType;
        var ex = Assert.Throws<ArgumentNullException>(() =>
            builder.WithProperty("prop", type!, "desc"));

        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public void Obsolete_WithProperty_StringOverload_AddsProperty()
    {
#pragma warning disable CS0618
        var toolName = "legacyTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithProperty("legacyProp", "string", "legacy description", required: true);
#pragma warning restore CS0618

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Single(options.Properties);
        var p = options.Properties[0];
        Assert.Equal("legacyProp", p.Name);
        Assert.Equal("string", p.Type);
        Assert.True(p.IsRequired);
        Assert.False(p.IsArray); // legacy overload cannot set arrays
    }

    [Fact]
    public void WithProperty_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("tool", out _);
        var result = builder.WithProperty("p", McpToolPropertyType.Object, "desc");

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithInputSchema_String_SetsInputSchema()
    {
        var toolName = "schemaTool";
        var builder = CreateBuilder(toolName, out var services);
        var schema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}},\"required\":[\"name\"]}";

        builder.WithInputSchema(schema);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Equal(schema, options.InputSchema);
    }

    [Fact]
    public void WithInputSchema_String_InvalidJson_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        Assert.ThrowsAny<JsonException>(() => builder.WithInputSchema("not valid json"));
    }

    [Fact]
    public void WithInputSchema_String_Empty_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        Assert.Throws<ArgumentException>(() => builder.WithInputSchema(string.Empty));
    }

    [Fact]
    public void WithInputSchema_String_NotObjectType_Throws()
    {
        var builder = CreateBuilder("tool", out _);
        var schema = "{\"type\":\"string\"}";

        Assert.Throws<ArgumentException>(() => builder.WithInputSchema(schema));
    }

    [Fact]
    public void WithInputSchema_String_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("tool", out _);
        var schema = "{\"type\":\"object\",\"properties\":{}}";

        var result = builder.WithInputSchema(schema);

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithInputSchema_Type_SetsInputSchemaFromClass()
    {
        var toolName = "typeTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithInputSchema(typeof(TestInput));

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.NotNull(options.InputSchema);

        var schemaNode = JsonNode.Parse(options.InputSchema)!.AsObject();
        Assert.Equal("object", schemaNode["type"]?.GetValue<string>());
        Assert.NotNull(schemaNode["properties"]);
    }

    [Fact]
    public void WithInputSchema_GenericType_SetsInputSchema()
    {
        var toolName = "genericTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithInputSchema<TestInput>();

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.NotNull(options.InputSchema);

        var schemaNode = JsonNode.Parse(options.InputSchema)!.AsObject();
        Assert.Equal("object", schemaNode["type"]?.GetValue<string>());
    }

    [Fact]
    public void WithInputSchema_Struct_SetsInputSchema()
    {
        var toolName = "structTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithInputSchema<TestStructInput>();

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.NotNull(options.InputSchema);

        var schemaNode = JsonNode.Parse(options.InputSchema)!.AsObject();
        Assert.Equal("object", schemaNode["type"]?.GetValue<string>());
        Assert.NotNull(schemaNode["properties"]);
    }

    [Fact]
    public void WithInputSchema_PrimitiveType_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithInputSchema(typeof(int)));
        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public void WithInputSchema_StringType_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithInputSchema(typeof(string)));
        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public void WithInputSchema_EnumType_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithInputSchema(typeof(DayOfWeek)));
        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public void WithInputSchema_InterfaceType_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithInputSchema(typeof(IDisposable)));
        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public void WithInputSchema_NullType_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        Assert.Throws<ArgumentNullException>(() => builder.WithInputSchema((Type)null!));
    }

    [Fact]
    public void WithInputSchema_Type_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("tool", out _);

        var result = builder.WithInputSchema<TestInput>();

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithInputSchema_Type_IncludesPropertyDescriptions()
    {
        var toolName = "descTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithInputSchema<TestInput>();

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        var schemaNode = JsonNode.Parse(options.InputSchema!)!.AsObject();
        var properties = schemaNode["properties"]!.AsObject();

        // City has [Description("The city name")] — should be included in the schema
        var cityProp = properties["city"];
        Assert.NotNull(cityProp);
        Assert.Equal("The city name", cityProp!["description"]?.GetValue<string>());
    }

    [Fact]
    public void WithInputSchema_Type_WithCustomSerializerOptions()
    {
        var toolName = "customOptsTool";
        var builder = CreateBuilder(toolName, out var services);

        var customOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };

        builder.WithInputSchema<TestInput>(customOptions);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.NotNull(options.InputSchema);
        var schemaNode = JsonNode.Parse(options.InputSchema!)!.AsObject();
        var properties = schemaNode["properties"]!.AsObject();

        // With camelCase naming policy, property names should be camelCase
        Assert.NotNull(properties["city"]);
    }

    [Fact]
    public void WithInputSchema_AfterWithProperty_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        builder.WithProperty("prop", McpToolPropertyType.String, "desc");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.WithInputSchema("{\"type\":\"object\",\"properties\":{}}"));

        Assert.Contains("mutually exclusive", ex.Message);
    }

    [Fact]
    public void WithInputSchema_Type_AfterWithProperty_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        builder.WithProperty("prop", McpToolPropertyType.String, "desc");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.WithInputSchema<TestInput>());

        Assert.Contains("mutually exclusive", ex.Message);
    }

    [Fact]
    public void WithProperty_AfterWithInputSchema_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        builder.WithInputSchema("{\"type\":\"object\",\"properties\":{}}");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.WithProperty("prop", McpToolPropertyType.String, "desc"));

        Assert.Contains("mutually exclusive", ex.Message);
    }

    [Fact]
    public void WithProperty_AfterWithInputSchema_Type_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        builder.WithInputSchema<TestInput>();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.WithProperty("prop", McpToolPropertyType.String, "desc"));

        Assert.Contains("mutually exclusive", ex.Message);
    }

    [Fact]
    public void WithProperty_MultipleCalls_Allowed()
    {
        var toolName = "multiProp";
        var builder = CreateBuilder(toolName, out var services);

        builder
            .WithProperty("a", McpToolPropertyType.String, "desc")
            .WithProperty("b", McpToolPropertyType.Integer, "desc");

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);
        Assert.Equal(2, options.Properties.Count);
    }

    [Fact]
    public void WithInputSchema_JsonNode_SetsInputSchema()
    {
        var toolName = "jsonNodeTool";
        var builder = CreateBuilder(toolName, out var services);
        var schemaNode = JsonNode.Parse("{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}},\"required\":[\"name\"]}")!;

        builder.WithInputSchema(schemaNode);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.NotNull(options.InputSchema);
        var parsed = JsonNode.Parse(options.InputSchema!)!.AsObject();
        Assert.Equal("object", parsed["type"]?.GetValue<string>());
        Assert.NotNull(parsed["properties"]);
    }

    [Fact]
    public void WithInputSchema_JsonNode_Null_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        Assert.Throws<ArgumentNullException>(() => builder.WithInputSchema((JsonNode)null!));
    }

    [Fact]
    public void WithInputSchema_JsonNode_NotObjectType_Throws()
    {
        var builder = CreateBuilder("tool", out _);
        var schemaNode = JsonNode.Parse("{\"type\":\"string\"}")!;

        Assert.Throws<ArgumentException>(() => builder.WithInputSchema(schemaNode));
    }

    [Fact]
    public void WithInputSchema_JsonNode_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("tool", out _);
        var schemaNode = JsonNode.Parse("{\"type\":\"object\",\"properties\":{}}")!;

        var result = builder.WithInputSchema(schemaNode);

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithInputSchema_JsonNode_AfterWithProperty_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        builder.WithProperty("prop", McpToolPropertyType.String, "desc");

        var schemaNode = JsonNode.Parse("{\"type\":\"object\",\"properties\":{}}")!;
        var ex = Assert.Throws<InvalidOperationException>(() => builder.WithInputSchema(schemaNode));

        Assert.Contains("mutually exclusive", ex.Message);
    }

    [Fact]
    public void WithProperty_AfterWithInputSchema_JsonNode_Throws()
    {
        var builder = CreateBuilder("tool", out _);
        var schemaNode = JsonNode.Parse("{\"type\":\"object\",\"properties\":{}}")!;

        builder.WithInputSchema(schemaNode);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.WithProperty("prop", McpToolPropertyType.String, "desc"));

        Assert.Contains("mutually exclusive", ex.Message);
    }

    [Fact]
    public void WithInputSchema_CalledTwice_UsesLast()
    {
        var toolName = "doubleSchema";
        var builder = CreateBuilder(toolName, out var services);

        var schema1 = "{\"type\":\"object\",\"properties\":{\"a\":{\"type\":\"string\"}}}";
        var schema2 = "{\"type\":\"object\",\"properties\":{\"b\":{\"type\":\"integer\"}}}";

        builder.WithInputSchema(schema1);
        builder.WithInputSchema(schema2);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Equal(schema2, options.InputSchema);
    }

    // ====== WithOutputSchema Tests ======

    [Fact]
    public void WithOutputSchema_String_SetsOutputSchema()
    {
        var toolName = "outputSchemaTool";
        var builder = CreateBuilder(toolName, out var services);
        var schema = "{\"type\":\"object\",\"properties\":{\"result\":{\"type\":\"string\"}},\"required\":[\"result\"]}";

        builder.WithOutputSchema(schema);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Equal(schema, options.OutputSchema);
    }

    [Fact]
    public void WithOutputSchema_String_InvalidJson_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        Assert.ThrowsAny<JsonException>(() => builder.WithOutputSchema("not valid json"));
    }

    [Fact]
    public void WithOutputSchema_String_Empty_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        Assert.Throws<ArgumentException>(() => builder.WithOutputSchema(string.Empty));
    }

    [Fact]
    public void WithOutputSchema_String_NotObjectType_Throws()
    {
        var builder = CreateBuilder("tool", out _);
        var schema = "{\"type\":\"string\"}";

        Assert.Throws<ArgumentException>(() => builder.WithOutputSchema(schema));
    }

    [Fact]
    public void WithOutputSchema_String_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("tool", out _);
        var schema = "{\"type\":\"object\",\"properties\":{}}";

        var result = builder.WithOutputSchema(schema);

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithOutputSchema_Type_SetsOutputSchemaFromClass()
    {
        var toolName = "outputTypeTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithOutputSchema(typeof(TestOutput));

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.NotNull(options.OutputSchema);

        var schemaNode = JsonNode.Parse(options.OutputSchema)!.AsObject();
        Assert.Equal("object", schemaNode["type"]?.GetValue<string>());
        Assert.NotNull(schemaNode["properties"]);
    }

    [Fact]
    public void WithOutputSchema_GenericType_SetsOutputSchema()
    {
        var toolName = "genericOutputTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithOutputSchema<TestOutput>();

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.NotNull(options.OutputSchema);

        var schemaNode = JsonNode.Parse(options.OutputSchema)!.AsObject();
        Assert.Equal("object", schemaNode["type"]?.GetValue<string>());
    }

    [Fact]
    public void WithOutputSchema_Struct_SetsOutputSchema()
    {
        var toolName = "structOutputTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithOutputSchema<TestStructOutput>();

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.NotNull(options.OutputSchema);

        var schemaNode = JsonNode.Parse(options.OutputSchema)!.AsObject();
        Assert.Equal("object", schemaNode["type"]?.GetValue<string>());
        Assert.NotNull(schemaNode["properties"]);
    }

    [Fact]
    public void WithOutputSchema_PrimitiveType_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithOutputSchema(typeof(int)));
        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public void WithOutputSchema_StringType_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithOutputSchema(typeof(string)));
        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public void WithOutputSchema_EnumType_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithOutputSchema(typeof(DayOfWeek)));
        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public void WithOutputSchema_InterfaceType_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        var ex = Assert.Throws<ArgumentException>(() => builder.WithOutputSchema(typeof(IDisposable)));
        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public void WithOutputSchema_NullType_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        Assert.Throws<ArgumentNullException>(() => builder.WithOutputSchema((Type)null!));
    }

    [Fact]
    public void WithOutputSchema_Type_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("tool", out _);

        var result = builder.WithOutputSchema<TestOutput>();

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithOutputSchema_JsonNode_SetsOutputSchema()
    {
        var toolName = "jsonNodeOutputTool";
        var builder = CreateBuilder(toolName, out var services);
        var schemaNode = JsonNode.Parse("{\"type\":\"object\",\"properties\":{\"status\":{\"type\":\"string\"}},\"required\":[\"status\"]}")!;

        builder.WithOutputSchema(schemaNode);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.NotNull(options.OutputSchema);
        var parsed = JsonNode.Parse(options.OutputSchema!)!.AsObject();
        Assert.Equal("object", parsed["type"]?.GetValue<string>());
        Assert.NotNull(parsed["properties"]);
    }

    [Fact]
    public void WithOutputSchema_JsonNode_Null_Throws()
    {
        var builder = CreateBuilder("tool", out _);

        Assert.Throws<ArgumentNullException>(() => builder.WithOutputSchema((JsonNode)null!));
    }

    [Fact]
    public void WithOutputSchema_JsonNode_NotObjectType_Throws()
    {
        var builder = CreateBuilder("tool", out _);
        var schemaNode = JsonNode.Parse("{\"type\":\"string\"}")!;

        Assert.Throws<ArgumentException>(() => builder.WithOutputSchema(schemaNode));
    }

    [Fact]
    public void WithOutputSchema_JsonNode_ReturnsSameBuilderInstance()
    {
        var builder = CreateBuilder("tool", out _);
        var schemaNode = JsonNode.Parse("{\"type\":\"object\",\"properties\":{}}")!;

        var result = builder.WithOutputSchema(schemaNode);

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithOutputSchema_CalledTwice_UsesLast()
    {
        var toolName = "doubleOutputSchema";
        var builder = CreateBuilder(toolName, out var services);

        var schema1 = "{\"type\":\"object\",\"properties\":{\"a\":{\"type\":\"string\"}}}";
        var schema2 = "{\"type\":\"object\",\"properties\":{\"b\":{\"type\":\"integer\"}}}";

        builder.WithOutputSchema(schema1);
        builder.WithOutputSchema(schema2);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Equal(schema2, options.OutputSchema);
    }

    [Fact]
    public void WithOutputSchema_CanBeUsedWithWithInputSchema()
    {
        var toolName = "bothSchemas";
        var builder = CreateBuilder(toolName, out var services);

        var inputSchema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}";
        var outputSchema = "{\"type\":\"object\",\"properties\":{\"result\":{\"type\":\"string\"}}}";

        builder.WithInputSchema(inputSchema).WithOutputSchema(outputSchema);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Equal(inputSchema, options.InputSchema);
        Assert.Equal(outputSchema, options.OutputSchema);
    }

    [Fact]
    public void WithOutputSchema_CanBeUsedWithWithProperty()
    {
        var toolName = "propertyAndOutput";
        var builder = CreateBuilder(toolName, out var services);

        var outputSchema = "{\"type\":\"object\",\"properties\":{\"result\":{\"type\":\"string\"}}}";

        builder
            .WithProperty("name", McpToolPropertyType.String, "name desc", required: true)
            .WithOutputSchema(outputSchema);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.Single(options.Properties);
        Assert.Equal(outputSchema, options.OutputSchema);
    }

    [Fact]
    public void WithOutputSchema_Type_WithCustomSerializerOptions()
    {
        var toolName = "customOptsOutputTool";
        var builder = CreateBuilder(toolName, out var services);

        var customOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };

        builder.WithOutputSchema<TestOutput>(customOptions);

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        Assert.NotNull(options.OutputSchema);
        var schemaNode = JsonNode.Parse(options.OutputSchema!)!.AsObject();
        var properties = schemaNode["properties"]!.AsObject();

        // With camelCase naming policy, property names should be camelCase
        Assert.NotNull(properties["status"]);
    }

    [Fact]
    public void WithOutputSchema_Type_IncludesPropertyDescriptions()
    {
        var toolName = "descOutputTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithOutputSchema<TestOutput>();

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        var schemaNode = JsonNode.Parse(options.OutputSchema!)!.AsObject();
        var properties = schemaNode["properties"]!.AsObject();

        // Status has [Description("The status message")] — should be included in the schema
        var statusProp = properties["status"];
        Assert.NotNull(statusProp);
        Assert.Equal("The status message", statusProp!["description"]?.GetValue<string>());
    }

    [Fact]
    public void WithOutputSchema_Type_RespectsSerializationAttributesAndInheritance()
    {
        var toolName = "annotatedOutputTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithOutputSchema<AnnotatedOutput>();

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        var schemaNode = JsonNode.Parse(options.OutputSchema!)!.AsObject();
        var properties = schemaNode["properties"]!.AsObject();

        Assert.NotNull(properties["baseStatus"]);
        Assert.NotNull(properties["renamedStatus"]);
        Assert.Null(properties["ignoredValue"]);
    }

    [Fact]
    public void WithOutputSchema_Type_UsesNullableMetadataForRequiredProperties()
    {
        var toolName = "nullableOutputTool";
        var builder = CreateBuilder(toolName, out var services);

        builder.WithOutputSchema<NullableAwareOutput>();

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<ToolOptions>>().Get(toolName);

        var schemaNode = JsonNode.Parse(options.OutputSchema!)!.AsObject();
        var required = schemaNode["required"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();

        Assert.Contains("requiredName", required);
        Assert.DoesNotContain("optionalDescription", required);
    }

    public class TestInput
    {
        [Description("The city name")]
        public string City { get; set; } = "";

        public int Days { get; set; }
    }

    public struct TestStructInput
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public class TestOutput
    {
        [Description("The status message")]
        public string Status { get; set; } = "";

        public bool Success { get; set; }
    }

    public struct TestStructOutput
    {
        public string Result { get; set; }
        public int Code { get; set; }
    }

    public class AnnotatedBaseOutput
    {
        public string BaseStatus { get; set; } = "";
    }

    public class AnnotatedOutput : AnnotatedBaseOutput
    {
        [JsonPropertyName("renamedStatus")]
        public string Status { get; set; } = "";

        [JsonIgnore]
        public string IgnoredValue { get; set; } = "";
    }

    public class NullableAwareOutput
    {
        public required string RequiredName { get; set; }

        public string? OptionalDescription { get; set; }
    }
}
