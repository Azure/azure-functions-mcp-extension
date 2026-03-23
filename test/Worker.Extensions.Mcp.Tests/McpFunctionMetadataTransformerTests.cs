// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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

        transformer.Transform([fn.Object]);
        fn.VerifyGet(f => f.Name, Times.Once);
    }

    [Fact]
    public void Transform_NonMatchingBinding_Ignored()
    {
        var transformer = CreateTransformer();
        var fn = CreateFunctionMetadata(entryPoint: null, scriptFile: null, name: "Func", bindings: ["{\"type\":\"httpTrigger\"}"]);
        transformer.Transform([fn.Object]);
        fn.VerifySet(f => f.RawBindings![It.IsAny<int>()] = It.IsAny<string>(), Times.Never);
    }

    [Fact]
    public void Transform_ResourceMetadata_WithJsonValue_ParsesCorrectly()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithResourceMetadataJson));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(
            entryPoint,
            scriptFile,
            "Func",
            ["{\"type\":\"mcpResourceTrigger\",\"uri\":\"file://test\",\"resourceName\":\"test\"}"]);

        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        var metadata = json["metadata"]!.GetValue<string>();
        
        // Should contain parsed JSON object with nested structure
        Assert.Contains("nested", metadata);
        Assert.Contains("config", metadata);
    }

    [Fact]
    public void Transform_ResourceMetadata_NoMetadataAttribute_NoMetadataAdded()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.NoAttributes));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(
            entryPoint,
            scriptFile,
            "Func",
            ["{\"type\":\"mcpResourceTrigger\",\"uri\":\"file://test\",\"resourceName\":\"test\"}"]);

        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        
        // Should not add metadata if no attributes present
        Assert.False(json.ContainsKey("metadata"));
    }

    [Fact]
    public void Transform_ToolMetadata_WithToolMetadataAttribute_ExtractsMetadata()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithToolMetadataJson));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(
            entryPoint,
            scriptFile,
            "Func",
            ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithToolMetadata\"}"]);

        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        
        Assert.True(json.ContainsKey("metadata"));
        var metadata = json["metadata"]!.GetValue<string>();
        Assert.NotEmpty(metadata);
        Assert.Contains("author", metadata);
    }

    [Fact]
    public void Transform_ToolMetadata_NoMetadataAttribute_NoMetadataAdded()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.NoAttributes));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var transformer = CreateTransformer();

        var fn = CreateFunctionMetadata(
            entryPoint,
            scriptFile,
            "Func",
            ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"NoAttributes\"}"]);

        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        
        // Should not add metadata if no attributes present
        Assert.False(json.ContainsKey("metadata"));
    }

    [Fact]
    public void Transform_ToolMetadata_FluentApiMetadata_AppliesMetadata()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.NoAttributes));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var toolOptions = new ToolOptions { Properties = [] };
        toolOptions.Metadata["version"] = "1.0";
        toolOptions.Metadata["author"] = "Test Author";

        var transformer = CreateTransformer(toolOptions: new Dictionary<string, ToolOptions>
        {
            ["NoAttributes"] = toolOptions,
        });

        var fn = CreateFunctionMetadata(
            entryPoint,
            scriptFile,
            "Func",
            ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"NoAttributes\"}"]);

        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();

        Assert.True(json.ContainsKey("metadata"));
        var metadata = json["metadata"]!.GetValue<string>();
        Assert.Contains("version", metadata);
        Assert.Contains("1.0", metadata);
        Assert.Contains("author", metadata);
        Assert.Contains("Test Author", metadata);
    }

    [Fact]
    public void Transform_ResourceMetadata_FluentApiMetadata_AppliesMetadata()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.NoAttributes));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var resourceOptions = new ResourceOptions();
        resourceOptions.Metadata["ui"] = new { prefersBorder = true };

        var transformer = CreateTransformer(resourceOptions: new Dictionary<string, ResourceOptions>
        {
            ["file://test"] = resourceOptions,
        });

        var fn = CreateFunctionMetadata(
            entryPoint,
            scriptFile,
            "Func",
            ["{\"type\":\"mcpResourceTrigger\",\"uri\":\"file://test\",\"resourceName\":\"test\"}"]);

        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();

        Assert.True(json.ContainsKey("metadata"));
        var metadata = json["metadata"]!.GetValue<string>();
        Assert.Contains("ui", metadata);
        Assert.Contains("prefersBorder", metadata);
    }

    [Fact]
    public void Transform_ToolMetadata_BothFluentAndAttributed_MergesMetadata()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithToolMetadataJson));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var toolOptions = new ToolOptions { Properties = [] };
        toolOptions.Metadata["version"] = "1.0";

        var transformer = CreateTransformer(toolOptions: new Dictionary<string, ToolOptions>
        {
            ["WithToolMetadata"] = toolOptions,
        });

        var fn = CreateFunctionMetadata(
            entryPoint,
            scriptFile,
            "Func",
            ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithToolMetadata\"}"]);

        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();

        Assert.True(json.ContainsKey("metadata"));
        var metadata = json["metadata"]!.GetValue<string>();
        Assert.Contains("version", metadata);
        Assert.Contains("1.0", metadata);
        Assert.Contains("author", metadata);
        Assert.Contains("Jane Doe", metadata);
    }

    [Fact]
    public void Transform_ResourceMetadata_BothFluentAndAttributed_MergesMetadata()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithResourceMetadataJson));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var resourceOptions = new ResourceOptions();
        resourceOptions.Metadata["version"] = "1.0";

        var transformer = CreateTransformer(resourceOptions: new Dictionary<string, ResourceOptions>
        {
            ["file://test"] = resourceOptions,
        });

        var fn = CreateFunctionMetadata(
            entryPoint,
            scriptFile,
            "Func",
            ["{\"type\":\"mcpResourceTrigger\",\"uri\":\"file://test\",\"resourceName\":\"test\"}"]);

        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();

        Assert.True(json.ContainsKey("metadata"));
        var metadata = json["metadata"]!.GetValue<string>();
        Assert.Contains("version", metadata);
        Assert.Contains("1.0", metadata);
        Assert.Contains("config", metadata);
        Assert.Contains("nested", metadata);
    }

    [Fact]
    public void MergeMetadata_BothNull_ReturnsEmptyObject()
    {
        var result = McpFunctionMetadataTransformer.MergeMetadata(null, null, out var overlapping);
        Assert.Equal("{}", result);
        Assert.Empty(overlapping);
    }

    [Fact]
    public void MergeMetadata_OverlappingKeys_AttributedWins()
    {
        var fluent = """{"shared":"fromFluent","onlyFluent":"kept"}""";
        var attributed = """{"shared":"fromAttributed","onlyAttr":"also"}""";
        var result = McpFunctionMetadataTransformer.MergeMetadata(fluent, attributed, out var overlapping);
        var node = JsonNode.Parse(result)!.AsObject();

        Assert.Equal("fromAttributed", node["shared"]!.GetValue<string>());
        Assert.Equal("kept", node["onlyFluent"]!.GetValue<string>());
        Assert.Equal("also", node["onlyAttr"]!.GetValue<string>());
        Assert.Single(overlapping);
        Assert.Contains("shared", overlapping);
    }

    private static IInputSchemaResolver[] CreateSchemaResolvers(IOptionsMonitor<ToolOptions> toolOptionsMonitor)
    {
        return
        [
            new ExplicitInputSchemaResolver(toolOptionsMonitor),
            new PropertyBasedInputSchemaResolver(toolOptionsMonitor),
            new ReflectionBasedInputSchemaResolver(NullLogger<ReflectionBasedInputSchemaResolver>.Instance),
        ];
    }

    private static McpFunctionMetadataTransformer CreateTransformer(
        IReadOnlyDictionary<string, ToolOptions>? toolOptions = null,
        IReadOnlyDictionary<string, ResourceOptions>? resourceOptions = null)
    {
        var toolOptionsMonitor = new Mock<IOptionsMonitor<ToolOptions>>();
        toolOptionsMonitor
            .Setup(o => o.Get(It.IsAny<string>()))
            .Returns((string name) => toolOptions is not null && toolOptions.TryGetValue(name, out var options)
                ? options
                : new ToolOptions { Properties = [] });

        var resourceOptionsMonitor = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptionsMonitor
            .Setup(o => o.Get(It.IsAny<string>()))
            .Returns((string name) => resourceOptions is not null && resourceOptions.TryGetValue(name, out var options)
                ? options
                : new ResourceOptions());

        return new McpFunctionMetadataTransformer(
            toolOptionsMonitor.Object,
            resourceOptionsMonitor.Object,
            CreateSchemaResolvers(toolOptionsMonitor.Object),
            NullLogger<McpFunctionMetadataTransformer>.Instance);
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

        public void WithResourceMetadataJson(
            [McpResourceTrigger("file://test", "test")]
            [McpMetadata("""{"config": {"nested": {"key": "value"}}}""")] ResourceInvocationContext context) { }

        public void WithToolMetadataJson(
            [McpToolTrigger("WithToolMetadata", "desc")]
            [McpMetadata("""{"author": "Jane Doe"}""")] ToolInvocationContext context) { }
    }
}
