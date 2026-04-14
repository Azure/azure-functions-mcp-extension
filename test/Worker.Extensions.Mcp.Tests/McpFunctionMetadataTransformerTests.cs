// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.McpApp;
using Microsoft.Extensions.Options;
using Moq;

namespace Worker.Extensions.Mcp.Tests;

public class McpFunctionMetadataTransformerTests : IDisposable
{
    private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
    }

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
    public void Transform_MatchingBinding_NoToolPropertiesConfiguredAndNoAttributes_FallsBackEmpty()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.NoAttributes));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

            var options = new Mock<IOptionsMonitor<ToolOptions>>();
            options.Setup(o => o.Get(It.IsAny<string>())).Returns(new ToolOptions { Properties = [] });
            var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
            resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());
            var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

            var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"NoAttributes\"}"]);

            transformer.Transform([fn.Object]);

            var binding = fn.Object.RawBindings![0];
            var json = JsonNode.Parse(binding)!.AsObject();
            var tp = json["toolProperties"]!.GetValue<string>();

            Assert.Equal("[]", tp);
    }

    [Fact]
    public void Transform_UsesConfiguredOptions_WhenAvailable()
    {
        var configuredProps = new List<ToolProperty>
        {
            new("x","string","desc", true)
        };

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get("MyTool")).Returns(new ToolOptions { Properties = configuredProps });
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());

        var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

        var fn = CreateFunctionMetadata(
            entryPoint: null,
            scriptFile: null,
            name: "Func",
            bindings: ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);

        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        var tp = json["toolProperties"]!.GetValue<string>();

        Assert.Contains("\"propertyName\":\"x\"", tp);
    }

    [Fact]
    public void Transform_Attribute_McpToolProperty_OnParameter()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithToolProperty));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

            var options = new Mock<IOptionsMonitor<ToolOptions>>();
            options.Setup(o => o.Get(It.IsAny<string>())).Returns(new ToolOptions { Properties = [] });
            var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
            resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());
            var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

            var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithToolProperty\"}"]);

            transformer.Transform([fn.Object]);
            var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
            var tp = json["toolProperties"]!.GetValue<string>();

            Assert.Equal("[{\"propertyName\":\"name\",\"propertyType\":\"string\",\"description\":\"Name value\",\"isRequired\":true,\"isArray\":false,\"enumValues\":[]}]", tp);
    }

    [Fact]
    public void Transform_Attribute_McpToolTrigger_PocoParameters()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithTriggerPoco));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

            var options = new Mock<IOptionsMonitor<ToolOptions>>();
            options.Setup(o => o.Get(It.IsAny<string>())).Returns(new ToolOptions { Properties = [] });
            var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
            resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());
            var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

            var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithTriggerPoco\"}"]);

            transformer.Transform([fn.Object]);
            var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
            var tp = json["toolProperties"]!.GetValue<string>();
            Assert.Contains("\"propertyName\":\"Content\"", tp);
            Assert.Contains("\"propertyName\":\"Title\"", tp);
        }

    [Fact]
    public void Transform_InvalidEntryPoint_NoChange()
    {
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, Path.GetDirectoryName(typeof(McpFunctionMetadataTransformerTests).Assembly.Location));

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>())).Returns(new ToolOptions { Properties = [] });
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());
        var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

        var fn = CreateFunctionMetadata("BadEntryPoint", "Some.dll", "Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"Bad\"}"]);
        fn.SetupGet(f => f.RawBindings).Returns(["{\"type\":\"mcpToolTrigger\",\"toolName\":\"Bad\"}"]);

        transformer.Transform([fn.Object]);
        var binding = fn.Object.RawBindings![0];
        var json = JsonNode.Parse(binding)!.AsObject();

        Assert.False(json.ContainsKey("toolProperties"));
    }

    [Fact]
    public void Transform_MethodNotFound_NoChange()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>("NonExistent");
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

            var options = new Mock<IOptionsMonitor<ToolOptions>>();
            options.Setup(o => o.Get(It.IsAny<string>())).Returns(new ToolOptions { Properties = [] });
            var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
            resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());
            var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

            var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"Whatever\"}"]);
            transformer.Transform([fn.Object]);
            var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();

            Assert.False(json.ContainsKey("toolProperties"));
        }

    [Fact]
    public void Transform_TypeNotFound_NoChange()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithToolProperty));
        // Corrupt the type portion
        entryPoint = entryPoint.Replace(typeof(TestFunctions).FullName!, typeof(McpFunctionMetadataTransformerTests).FullName! + "Missing");
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

            var options = new Mock<IOptionsMonitor<ToolOptions>>();
            options.Setup(o => o.Get(It.IsAny<string>())).Returns(new ToolOptions { Properties = [] });
            var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
            resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());
            var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

            var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithToolProperty\"}"]);
            transformer.Transform([fn.Object]);
            var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();

            Assert.False(json.ContainsKey("toolProperties"));
        }

    [Fact]
    public void Transform_ToolInvocationContextParameter_IgnoredForPocoProperties()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithContextAndPoco));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>())).Returns(new ToolOptions { Properties = [] });
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());
        var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

        var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithContextAndPoco\"}"]);
        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        var tp = json["toolProperties"]!.GetValue<string>();

        // Should have only the property from the POCO, not the context parameter
        Assert.DoesNotContain("mcptoolcontext", tp, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"propertyName\":\"Name\"", tp);
    }

    [Fact]
    public void Transform_ResourceMetadata_WithJsonValue_ParsesCorrectly()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithResourceMetadataJson));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());
        var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

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

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());
        var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

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

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>())).Returns(new ToolOptions { Properties = [] });
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());
        var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

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

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>())).Returns(new ToolOptions { Properties = [] });
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());
        var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

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

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get("NoAttributes")).Returns(toolOptions);
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());
        var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

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

        var resourceOpts = new ResourceOptions();
        resourceOpts.Metadata["ui"] = new { prefersBorder = true };

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>())).Returns(new ToolOptions { Properties = [] });
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get("file://test")).Returns(resourceOpts);
        var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

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

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get("WithToolMetadata")).Returns(toolOptions);
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());
        var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

        var fn = CreateFunctionMetadata(
            entryPoint,
            scriptFile,
            "Func",
            ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithToolMetadata\"}"]);

        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();

        Assert.True(json.ContainsKey("metadata"));
        var metadata = json["metadata"]!.GetValue<string>();
        // Fluent API metadata
        Assert.Contains("version", metadata);
        Assert.Contains("1.0", metadata);
        // Attributed metadata
        Assert.Contains("author", metadata);
        Assert.Contains("Jane Doe", metadata);
    }

    [Fact]
    public void Transform_ResourceMetadata_BothFluentAndAttributed_MergesMetadata()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithResourceMetadataJson));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var resourceOpts = new ResourceOptions();
        resourceOpts.Metadata["version"] = "1.0";

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>())).Returns(new ToolOptions { Properties = [] });
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get("file://test")).Returns(resourceOpts);
        var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
            promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
            var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

        var fn = CreateFunctionMetadata(
            entryPoint,
            scriptFile,
            "Func",
            ["{\"type\":\"mcpResourceTrigger\",\"uri\":\"file://test\",\"resourceName\":\"test\"}"]);

        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();

        Assert.True(json.ContainsKey("metadata"));
        var metadata = json["metadata"]!.GetValue<string>();
        // Fluent API metadata
        Assert.Contains("version", metadata);
        Assert.Contains("1.0", metadata);
        // Attributed metadata
        Assert.Contains("config", metadata);
        Assert.Contains("nested", metadata);
    }

    [Fact]
    public void MergeMetadata_BothNull_ReturnsEmptyObject()
    {
        var result = MetadataMerger.MergeMetadata(null, null, out var overlapping);
        Assert.Equal("{}", result);
        Assert.Empty(overlapping);
    }

    [Fact]
    public void MergeMetadata_FluentOnly_ReturnsFluent()
    {
        var fluent = """{"key1":"value1"}""";
        var result = MetadataMerger.MergeMetadata(fluent, null, out var overlapping);
        var node = JsonNode.Parse(result)!.AsObject();
        Assert.Equal("value1", node["key1"]!.GetValue<string>());
        Assert.Empty(overlapping);
    }

    [Fact]
    public void MergeMetadata_AttributedOnly_ReturnsAttributed()
    {
        var attributed = """{"key1":"value1"}""";
        var result = MetadataMerger.MergeMetadata(null, attributed, out var overlapping);
        var node = JsonNode.Parse(result)!.AsObject();
        Assert.Equal("value1", node["key1"]!.GetValue<string>());
        Assert.Empty(overlapping);
    }

    [Fact]
    public void MergeMetadata_DisjointKeys_MergesBoth()
    {
        var fluent = """{"fluentKey":"fluentValue"}""";
        var attributed = """{"attrKey":"attrValue"}""";
        var result = MetadataMerger.MergeMetadata(fluent, attributed, out var overlapping);
        var node = JsonNode.Parse(result)!.AsObject();
        Assert.Equal("fluentValue", node["fluentKey"]!.GetValue<string>());
        Assert.Equal("attrValue", node["attrKey"]!.GetValue<string>());
        Assert.Empty(overlapping);
    }

    [Fact]
    public void MergeMetadata_OverlappingKeys_AttributedWins()
    {
        var fluent = """{"shared":"fromFluent","onlyFluent":"kept"}""";
        var attributed = """{"shared":"fromAttributed","onlyAttr":"also"}""";
        var result = MetadataMerger.MergeMetadata(fluent, attributed, out var overlapping);
        var node = JsonNode.Parse(result)!.AsObject();
        Assert.Equal("fromAttributed", node["shared"]!.GetValue<string>());
        Assert.Equal("kept", node["onlyFluent"]!.GetValue<string>());
        Assert.Equal("also", node["onlyAttr"]!.GetValue<string>());
        Assert.Single(overlapping);
        Assert.Contains("shared", overlapping);
    }

    [Fact]
    public void MergeMetadata_MultipleOverlappingKeys_ReportsAll()
    {
        var fluent = """{"a":"1","b":"2","c":"3"}""";
        var attributed = """{"a":"x","b":"y"}""";
        var result = MetadataMerger.MergeMetadata(fluent, attributed, out var overlapping);
        var node = JsonNode.Parse(result)!.AsObject();
        Assert.Equal("x", node["a"]!.GetValue<string>());
        Assert.Equal("y", node["b"]!.GetValue<string>());
        Assert.Equal("3", node["c"]!.GetValue<string>());
        Assert.Equal(2, overlapping.Count);
        Assert.Contains("a", overlapping);
        Assert.Contains("b", overlapping);
    }

    [Fact]
    public void Transform_ToolWithAppOptions_SetsMetaUiOnBinding()
    {
        var appOptions = new AppOptions();
        appOptions.Visibility = McpVisibility.Model | McpVisibility.App;
        appOptions.View = new ViewOptions
        {
            Source = McpViewSource.FromFile("ui/app.html")
        };

        var toolOptions = new ToolOptions
        {
            Properties = new List<ToolProperty> { new("x", "string", "desc", true) },
            AppOptions = appOptions
        };

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get("UiTool")).Returns(toolOptions);
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());

        var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, new Mock<IOptionsMonitor<PromptOptions>>().Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

        var fn = CreateFunctionMetadata(null, null, "Func",
            ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"UiTool\"}"]);

        var list = new List<IFunctionMetadata> { fn.Object };
        transformer.Transform(list);

        // Verify the binding JSON now contains a "metadata" property with "ui" inside
        var binding = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.True(binding.ContainsKey("metadata"));

        var metadataStr = binding["metadata"]!.GetValue<string>();
        var metaObj = JsonNode.Parse(metadataStr)!.AsObject();
        Assert.True(metaObj.ContainsKey("ui"));

        var ui = metaObj["ui"]!.AsObject();
        Assert.Equal("ui://UiTool/view", ui["resourceUri"]!.GetValue<string>());

        var visibility = ui["visibility"]!.AsArray();
        Assert.Contains("model", visibility.Select(v => v!.GetValue<string>()));
        Assert.Contains("app", visibility.Select(v => v!.GetValue<string>()));
    }

    [Fact]
    public void Transform_ToolWithAppOptions_MergesWithExistingMetadata()
    {
        var appOptions = new AppOptions();
        appOptions.View = new ViewOptions
        {
            Source = McpViewSource.FromFile("ui/app.html")
        };

        var toolOptions = new ToolOptions
        {
            Properties = new List<ToolProperty> { new("x", "string", "desc", true) },
            AppOptions = appOptions
        };

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get("MergeTool")).Returns(toolOptions);
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());

        var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, new Mock<IOptionsMonitor<PromptOptions>>().Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

        // Binding already has metadata with "author" key (from [McpMetadata])
        var fn = CreateFunctionMetadata(null, null, "Func",
            ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MergeTool\",\"metadata\":\"{\\\"author\\\":\\\"Jane\\\"}\"}"]);

        var list = new List<IFunctionMetadata> { fn.Object };
        transformer.Transform(list);

        var binding = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        var metadataStr = binding["metadata"]!.GetValue<string>();
        var metaObj = JsonNode.Parse(metadataStr)!.AsObject();

        // Both original "author" and new "ui" should be present
        Assert.Equal("Jane", metaObj["author"]!.GetValue<string>());
        Assert.True(metaObj.ContainsKey("ui"));
        Assert.Equal("ui://MergeTool/view", metaObj["ui"]!.AsObject()["resourceUri"]!.GetValue<string>());
    }

    [Fact]
    public void Transform_ToolWithAppOptions_EmitsSyntheticResourceFunction()
    {
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, Path.GetDirectoryName(typeof(McpFunctionMetadataTransformerTests).Assembly.Location));

        var appOptions = new AppOptions();
        appOptions.View = new ViewOptions
        {
            Source = McpViewSource.FromFile("ui/app.html")
        };

        var toolOptions = new ToolOptions
        {
            Properties = [],
            AppOptions = appOptions
        };

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get("MyAppTool")).Returns(toolOptions);
        options.Setup(o => o.Get(It.Is<string>(s => s != "MyAppTool")))
               .Returns(new ToolOptions { Properties = [] });
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());

        var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, new Mock<IOptionsMonitor<PromptOptions>>().Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

        var fn = CreateFunctionMetadata(null, null, "Func",
            ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyAppTool\"}"]);

        var list = new List<IFunctionMetadata> { fn.Object };
        transformer.Transform(list);

        // Should have the original function + 1 synthetic resource function
        Assert.Equal(2, list.Count);

        var synthetic = list[1];
        Assert.Equal("functions--mcpapp-MyAppTool", synthetic.Name);
        Assert.Equal("dotnet-isolated", synthetic.Language);
        Assert.NotNull(synthetic.RawBindings);
        Assert.True(McpAppUtilities.IsSyntheticFunction("functions--mcpapp-MyAppTool"));
    }

    [Fact]
    public void Transform_ToolWithStaticAssets_EmitsSingleResourceFunction()
    {
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, Path.GetDirectoryName(typeof(McpFunctionMetadataTransformerTests).Assembly.Location));

        var appOptions = new AppOptions();
        appOptions.View = new ViewOptions
        {
            Source = McpViewSource.FromFile("ui/app.html")
        };
        appOptions.StaticAssetsDirectory = "ui/dist";

        var toolOptions = new ToolOptions
        {
            Properties = [],
            AppOptions = appOptions
        };

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get("AssetTool")).Returns(toolOptions);
        options.Setup(o => o.Get(It.Is<string>(s => s != "AssetTool")))
               .Returns(new ToolOptions { Properties = [] });
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());

        var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, new Mock<IOptionsMonitor<PromptOptions>>().Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

        var fn = CreateFunctionMetadata(null, null, "Func",
            ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"AssetTool\"}"]);

        var list = new List<IFunctionMetadata> { fn.Object };
        transformer.Transform(list);

        // Per MCP Apps spec, only resource function is emitted (no separate static assets)
        Assert.Equal(2, list.Count);
        Assert.Equal("functions--mcpapp-AssetTool", list[1].Name);
    }

    [Fact]
    public void Transform_ToolWithoutAppOptions_NoSyntheticFunctions()
    {
        var configuredProps = new List<ToolProperty>
        {
            new("x", "string", "desc", true)
        };

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>()))
               .Returns(new ToolOptions { Properties = configuredProps });
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());

        var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, new Mock<IOptionsMonitor<PromptOptions>>().Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

        var fn = CreateFunctionMetadata(null, null, "Func",
            ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"RegularTool\"}"]);

        var list = new List<IFunctionMetadata> { fn.Object };
        transformer.Transform(list);

        Assert.Single(list);
    }

    [Fact]
    public void Transform_DuplicateToolNames_EmitsSingleSyntheticFunction()
    {
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, Path.GetDirectoryName(typeof(McpFunctionMetadataTransformerTests).Assembly.Location));

        var appOptions = new AppOptions();
        appOptions.View = new ViewOptions
        {
            Source = McpViewSource.FromFile("ui/app.html")
        };

        var toolOptions = new ToolOptions
        {
            Properties = [],
            AppOptions = appOptions
        };

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get("DupeTool")).Returns(toolOptions);
        options.Setup(o => o.Get(It.Is<string>(s => s != "DupeTool")))
               .Returns(new ToolOptions { Properties = [] });
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());

        var transformer = new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, new Mock<IOptionsMonitor<PromptOptions>>().Object, NullLogger<McpFunctionMetadataTransformer>.Instance);

        var fn1 = CreateFunctionMetadata(null, null, "Func1",
            ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"DupeTool\"}"]);
        var fn2 = CreateFunctionMetadata(null, null, "Func2",
            ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"DupeTool\"}"]);

        var list = new List<IFunctionMetadata> { fn1.Object, fn2.Object };
        transformer.Transform(list);

        // Should have 2 original functions + only 1 synthetic (deduplicated)
        Assert.Equal(3, list.Count);
        Assert.Single(list, f => f.Name == "functions--mcpapp-DupeTool");
    }

    private static McpFunctionMetadataTransformer CreateTransformer(List<ToolProperty>? configured = null)
    {
        var options =  new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>()))
            .Returns(new ToolOptions { Properties = configured ?? [] });
        var resourceOptions = new Mock<IOptionsMonitor<ResourceOptions>>();
        resourceOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new ResourceOptions());
        var promptOptions = new Mock<IOptionsMonitor<PromptOptions>>();
        promptOptions.Setup(o => o.Get(It.IsAny<string>())).Returns(new PromptOptions());
        return new McpFunctionMetadataTransformer(options.Object, resourceOptions.Object, promptOptions.Object, NullLogger<McpFunctionMetadataTransformer>.Instance);
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

        public void WithResourceMetadataJson(
            [McpResourceTrigger("file://test", "test")]
            [McpMetadata("""{"config": {"nested": {"key": "value"}}}""")] ResourceInvocationContext context) { }

        public void WithToolMetadataJson(
            [McpToolTrigger("WithToolMetadata", "desc")]
            [McpMetadata("""{"author": "Jane Doe"}""")] ToolInvocationContext context) { }
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
}
