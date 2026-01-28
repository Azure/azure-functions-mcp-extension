using System.ComponentModel;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
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
    public void Transform_MatchingBinding_NoToolPropertiesConfiguredAndNoAttributes_FallsBackEmpty()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.NoAttributes));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>())).Returns(new ToolOptions { Properties = [] });
        var transformer = new McpFunctionMetadataTransformer(options.Object);

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

        var transformer = new McpFunctionMetadataTransformer(options.Object);

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
        var transformer = new McpFunctionMetadataTransformer(options.Object);

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
        var transformer = new McpFunctionMetadataTransformer(options.Object);

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
        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>())).Returns(new ToolOptions { Properties = [] });
        var transformer = new McpFunctionMetadataTransformer(options.Object);

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
        var transformer = new McpFunctionMetadataTransformer(options.Object);

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
        var transformer = new McpFunctionMetadataTransformer(options.Object);

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
        var transformer = new McpFunctionMetadataTransformer(options.Object);

        var fn = CreateFunctionMetadata(entryPoint, scriptFile, "Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"WithContextAndPoco\"}"]);
        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        var tp = json["toolProperties"]!.GetValue<string>();

        // Should have only the property from the POCO, not the context parameter
        Assert.DoesNotContain("mcptoolcontext", tp, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"propertyName\":\"Name\"", tp);
    }

    [Fact]
    public void Transform_ResourceMetadata_WithResourceMetadataAttribute_ExtractsMetadata()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithResourceMetadata));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        var transformer = new McpFunctionMetadataTransformer(options.Object);

        var fn = CreateFunctionMetadata(
            entryPoint,
            scriptFile,
            "Func",
            ["{\"type\":\"mcpResourceTrigger\",\"uri\":\"file://test\",\"resourceName\":\"test\"}"]);

        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        
        Assert.True(json.ContainsKey("metadata"));
        var metadata = json["metadata"]!.GetValue<string>();
        Assert.NotEmpty(metadata);
    }

    [Fact]
    public void Transform_ResourceMetadata_MultipleAttributes_CombinesAllMetadata()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithMultipleResourceMetadata));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        var transformer = new McpFunctionMetadataTransformer(options.Object);

        var fn = CreateFunctionMetadata(
            entryPoint,
            scriptFile,
            "Func",
            ["{\"type\":\"mcpResourceTrigger\",\"uri\":\"file://test\",\"resourceName\":\"test\"}"]);

        transformer.Transform([fn.Object]);
        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        
        Assert.True(json.ContainsKey("metadata"));
        var metadata = json["metadata"]!.GetValue<string>();
        Assert.Contains("\"prop1\"", metadata);
        Assert.Contains("\"prop2\"", metadata);
    }

    [Fact]
    public void Transform_ResourceMetadata_WithJsonValue_ParsesCorrectly()
    {
        var (entryPoint, scriptFile, outputDir) = GetFunctionMetadataInfo<TestFunctions>(nameof(TestFunctions.WithResourceMetadataJson));
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        var transformer = new McpFunctionMetadataTransformer(options.Object);

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
        var transformer = new McpFunctionMetadataTransformer(options.Object);

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

    private static McpFunctionMetadataTransformer CreateTransformer(List<ToolProperty>? configured = null)
    {
        var options =  new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>()))
            .Returns(new ToolOptions { Properties = configured ?? [] });
        return new McpFunctionMetadataTransformer(options.Object);
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

        public void WithResourceMetadata(
            [McpResourceTrigger("file://test", "test")]
            [McpMetadata("""{"prop1": "value1"}""")] ResourceInvocationContext context) { }

        public void WithMultipleResourceMetadata(
            [McpResourceTrigger("file://test", "test")]
            [McpMetadata("""{"prop1": "value1", "prop2": "value2"}""")] ResourceInvocationContext context) { }

        public void WithResourceMetadataJson(
            [McpResourceTrigger("file://test", "test")]
            [McpMetadata("""{"config": {"nested": {"key": "value"}}}""")] ResourceInvocationContext context) { }
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
