using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Worker.Extensions.Mcp.Tests.Helpers;

/// <summary>
/// Shared factory methods for builder and transformer tests.
/// </summary>
internal static class BuilderTestHelper
{
    public static McpBindingBuilder CreateBuilder(params string[] bindings)
    {
        var fn = CreateFunctionMetadata(bindings: bindings);
        return new McpBindingBuilder(fn.Object, NullLogger.Instance, CreateToolOptions(), CreateResourceOptions(), CreatePromptOptions());
    }

    public static McpBindingBuilder CreateBuilder(
        IOptionsMonitor<ToolOptions> toolOptions,
        IOptionsMonitor<ResourceOptions> resourceOptions,
        IOptionsMonitor<PromptOptions> promptOptions,
        params string[] bindings)
    {
        var fn = CreateFunctionMetadata(bindings: bindings);
        return new McpBindingBuilder(fn.Object, NullLogger.Instance, toolOptions, resourceOptions, promptOptions);
    }

    public static Mock<IFunctionMetadata> CreateFunctionMetadata(
        string? entryPoint = null,
        string? scriptFile = null,
        string? name = "TestFunc",
        IList<string>? bindings = null)
    {
        var fn = new Mock<IFunctionMetadata>();
        fn.SetupGet(f => f.EntryPoint).Returns(entryPoint);
        fn.SetupGet(f => f.ScriptFile).Returns(scriptFile);
        fn.SetupGet(f => f.Name).Returns(name);
        fn.SetupGet(f => f.RawBindings).Returns(bindings);
        return fn;
    }

    public static IOptionsMonitor<ToolOptions> CreateToolOptions(
        string? toolName = null,
        List<ToolProperty>? properties = null,
        Dictionary<string, object>? metadata = null,
        string? inputSchema = null)
    {
        var mock = new Mock<IOptionsMonitor<ToolOptions>>();
        var defaultOptions = new ToolOptions { Properties = [] };

        if (toolName is not null)
        {
            var namedOptions = new ToolOptions { Properties = properties ?? [], InputSchema = inputSchema };
            PopulateMetadata(namedOptions, metadata);
            mock.Setup(o => o.Get(toolName)).Returns(namedOptions);
            mock.Setup(o => o.Get(It.Is<string>(s => s != toolName))).Returns(defaultOptions);
        }
        else
        {
            mock.Setup(o => o.Get(It.IsAny<string>())).Returns(defaultOptions);
        }

        return mock.Object;
    }

    public static IOptionsMonitor<ResourceOptions> CreateResourceOptions(
        string? name = null,
        Dictionary<string, object>? metadata = null,
        string? inputSchema = null)
    {
        var mock = new Mock<IOptionsMonitor<ResourceOptions>>();
        var defaultOptions = new ResourceOptions();

        if (name is not null)
        {
            var namedOptions = new ResourceOptions { InputSchema = inputSchema };
            PopulateMetadata(namedOptions, metadata);
            mock.Setup(o => o.Get(name)).Returns(namedOptions);
            mock.Setup(o => o.Get(It.Is<string>(s => s != name))).Returns(defaultOptions);
        }
        else
        {
            mock.Setup(o => o.Get(It.IsAny<string>())).Returns(defaultOptions);
        }

        return mock.Object;
    }

    public static IOptionsMonitor<PromptOptions> CreatePromptOptions(
        string? promptName = null,
        List<PromptArgumentDefinition>? arguments = null,
        Dictionary<string, object>? metadata = null,
        string? inputSchema = null)
    {
        var mock = new Mock<IOptionsMonitor<PromptOptions>>();
        var defaultOptions = new PromptOptions();

        if (promptName is not null)
        {
            var namedOptions = new PromptOptions { InputSchema = inputSchema };
            if (arguments is not null)
            {
                foreach (var arg in arguments)
                {
                    namedOptions.AddArgument(arg.Name, arg.Description, arg.Required);
                }
            }
            PopulateMetadata(namedOptions, metadata);
            mock.Setup(o => o.Get(promptName)).Returns(namedOptions);
            mock.Setup(o => o.Get(It.Is<string>(s => s != promptName))).Returns(defaultOptions);
        }
        else
        {
            mock.Setup(o => o.Get(It.IsAny<string>())).Returns(defaultOptions);
        }

        return mock.Object;
    }

    public static (string EntryPoint, string ScriptFile, string OutputDir) GetFunctionMetadataInfo<T>(string methodName)
    {
        var type = typeof(T);
        var entryPoint = $"{type.FullName}.{methodName}";
        var scriptFile = Path.GetFileName(type.Assembly.Location);
        var outputDir = Path.GetDirectoryName(type.Assembly.Location)!;
        return (entryPoint, scriptFile, outputDir);
    }

    private static void PopulateMetadata(McpBuilderOptions options, Dictionary<string, object>? metadata)
    {
        if (metadata is null) return;
        foreach (var kvp in metadata)
        {
            options.Metadata[kvp.Key] = kvp.Value;
        }
    }
}
