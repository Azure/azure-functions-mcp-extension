// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Worker.Extensions.Mcp.Tests;

public class McpOutputSchemaTransformerTests
{
    private static McpOutputSchemaTransformer CreateTransformer(
        IFunctionMethodResolver? methodResolver = null,
        Action<Mock<IOptionsMonitor<ToolOptions>>>? configureOptions = null,
        ILogger<McpOutputSchemaTransformer>? logger = null)
    {
        var options = new Mock<IOptionsMonitor<ToolOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>())).Returns(new ToolOptions { Properties = [] });
        configureOptions?.Invoke(options);

        return new McpOutputSchemaTransformer(
            methodResolver ?? Mock.Of<IFunctionMethodResolver>(),
            options.Object,
            logger ?? NullLogger<McpOutputSchemaTransformer>.Instance);
    }

    private static Mock<IFunctionMetadata> CreateFunctionMetadata(string? name, IList<string>? bindings)
    {
        var fn = new Mock<IFunctionMetadata>();
        fn.SetupGet(f => f.Name).Returns(name);
        fn.SetupGet(f => f.RawBindings).Returns(bindings);
        return fn;
    }

    private static Mock<IFunctionMethodResolver> CreateMethodResolverFor(string testMethodName)
    {
        var method = typeof(TestFunctions).GetMethod(testMethodName, BindingFlags.Public | BindingFlags.Instance)!;
        var resolver = new Mock<IFunctionMethodResolver>();
        resolver.Setup(r => r.TryResolveMethod(It.IsAny<IFunctionMetadata>(), out method))
            .Returns(true);
        return resolver;
    }

    #region General transform behavior

    [Fact]
    public void Transform_NullOriginal_DoesNothing()
    {
        var transformer = CreateTransformer();
        transformer.Transform(null!);
    }

    [Fact]
    public void Transform_EmptyList_DoesNothing()
    {
        var transformer = CreateTransformer();
        var list = new List<IFunctionMetadata>();
        transformer.Transform(list);
        Assert.Empty(list);
    }

    [Fact]
    public void Transform_NullRawBindings_Skips()
    {
        var transformer = CreateTransformer();
        var fn = CreateFunctionMetadata("Func", null);
        transformer.Transform([fn.Object]);
    }

    [Fact]
    public void Transform_EmptyRawBindings_Skips()
    {
        var transformer = CreateTransformer();
        var fn = CreateFunctionMetadata("Func", []);
        transformer.Transform([fn.Object]);
    }

    [Fact]
    public void Transform_NonToolTriggerBinding_Ignored()
    {
        var transformer = CreateTransformer();
        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"httpTrigger\"}"]);
        transformer.Transform([fn.Object]);
        Assert.Equal("{\"type\":\"httpTrigger\"}", fn.Object.RawBindings![0]);
    }

    #endregion

    #region Explicit output schema (fluent API)

    [Fact]
    public void Transform_InjectsExplicitOutputSchema_WhenConfigured()
    {
        var outputSchema = "{\"type\":\"object\",\"properties\":{\"result\":{\"type\":\"string\"}}}";

        var transformer = CreateTransformer(configureOptions: options =>
        {
            options.Setup(o => o.Get("MyTool"))
                .Returns(new ToolOptions { Properties = [], OutputSchema = outputSchema });
        });

        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);
        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.True(json.ContainsKey("outputSchema"));
        Assert.Equal(outputSchema, json["outputSchema"]?.GetValue<string>());
    }

    [Fact]
    public void Transform_NoOutputSchema_WhenNotConfigured()
    {
        var transformer = CreateTransformer();
        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);

        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.False(json.ContainsKey("outputSchema"));
    }

    [Fact]
    public void Transform_NoToolName_NoOutputSchema()
    {
        var outputSchema = "{\"type\":\"object\",\"properties\":{}}";
        var transformer = CreateTransformer(configureOptions: options =>
        {
            options.Setup(o => o.Get(It.IsAny<string>()))
                .Returns(new ToolOptions { Properties = [], OutputSchema = outputSchema });
        });

        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\"}"]);
        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.False(json.ContainsKey("outputSchema"));
    }

    [Fact]
    public void Transform_DoesNotSetUseResultSchema()
    {
        var outputSchema = "{\"type\":\"object\",\"properties\":{\"status\":{\"type\":\"string\"}}}";

        var transformer = CreateTransformer(configureOptions: options =>
        {
            options.Setup(o => o.Get("MyTool"))
                .Returns(new ToolOptions { Properties = [], OutputSchema = outputSchema });
        });

        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);
        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.False(json.ContainsKey("useResultSchema"));
        Assert.Equal(outputSchema, json["outputSchema"]?.GetValue<string>());
    }

    [Fact]
    public void Transform_ExplicitOutputSchema_TakesPrecedenceOverAutoGeneration()
    {
        var explicitSchema = "{\"type\":\"object\",\"properties\":{\"custom\":{\"type\":\"string\"}}}";
        var resolver = CreateMethodResolverFor(nameof(TestFunctions.ReturnsWeatherResult));
        var testLogger = new TestLogger<McpOutputSchemaTransformer>();

        var transformer = CreateTransformer(resolver.Object, options =>
        {
            options.Setup(o => o.Get("MyTool"))
                .Returns(new ToolOptions { Properties = [], OutputSchema = explicitSchema });
        }, testLogger);

        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);
        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.True(json.ContainsKey("outputSchema"));
        // Explicit schema wins, not the auto-generated one
        Assert.Equal(explicitSchema, json["outputSchema"]?.GetValue<string>());

        // Verify a warning was logged about both being defined
        Assert.Contains(testLogger.LogEntries, e =>
            e.LogLevel == LogLevel.Warning
            && e.Message.Contains("WithOutputSchema")
            && e.Message.Contains("[McpOutput]"));
    }

    #endregion

    #region Auto-generated output schema (McpContent)

    [Fact]
    public void Transform_McpContentReturnType_AutoGeneratesOutputSchema()
    {
        var resolver = CreateMethodResolverFor(nameof(TestFunctions.ReturnsWeatherResult));
        var transformer = CreateTransformer(resolver.Object);

        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);
        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.True(json.ContainsKey("outputSchema"));

        var schemaString = json["outputSchema"]!.GetValue<string>();
        var schema = JsonDocument.Parse(schemaString).RootElement;
        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.True(schema.TryGetProperty("properties", out var properties));
        Assert.True(properties.TryGetProperty("temperature", out _));
        Assert.True(properties.TryGetProperty("condition", out _));
    }

    [Fact]
    public void Transform_AutoGeneratedOutputSchema_IncludesPropertyDescriptions()
    {
        var resolver = CreateMethodResolverFor(nameof(TestFunctions.ReturnsWeatherResult));
        var transformer = CreateTransformer(resolver.Object);

        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);
        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        var schemaString = json["outputSchema"]!.GetValue<string>();
        var schema = JsonDocument.Parse(schemaString).RootElement;
        var properties = schema.GetProperty("properties");

        var temperatureProp = properties.GetProperty("temperature");
        Assert.True(temperatureProp.TryGetProperty("description", out var tempDesc));
        Assert.Equal("Temperature in celsius", tempDesc.GetString());

        var conditionProp = properties.GetProperty("condition");
        Assert.True(conditionProp.TryGetProperty("description", out var condDesc));
        Assert.Equal("Weather conditions description", condDesc.GetString());
    }

    [Fact]
    public void Transform_AsyncMcpContentReturnType_AutoGeneratesOutputSchema()
    {
        var resolver = CreateMethodResolverFor(nameof(TestFunctions.ReturnsWeatherResultAsync));
        var transformer = CreateTransformer(resolver.Object);

        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);
        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.True(json.ContainsKey("outputSchema"));

        var schemaString = json["outputSchema"]!.GetValue<string>();
        var schema = JsonDocument.Parse(schemaString).RootElement;
        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.True(schema.TryGetProperty("properties", out var properties));
        Assert.True(properties.TryGetProperty("temperature", out _));
        Assert.True(properties.TryGetProperty("condition", out _));
    }

    [Fact]
    public void Transform_ValueTaskMcpContentReturnType_AutoGeneratesOutputSchema()
    {
        var resolver = CreateMethodResolverFor(nameof(TestFunctions.ReturnsWeatherResultValueTask));
        var transformer = CreateTransformer(resolver.Object);

        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);
        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.True(json.ContainsKey("outputSchema"));
    }

    [Fact]
    public void Transform_McpContentOnlyReturnType_NoOutputSchema()
    {
        var resolver = CreateMethodResolverFor(nameof(TestFunctions.ReturnsContentOnlyResult));
        var transformer = CreateTransformer(resolver.Object);

        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);
        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.False(json.ContainsKey("outputSchema"));
    }

    [Fact]
    public void Transform_NonMcpContentReturnType_NoOutputSchema()
    {
        var resolver = CreateMethodResolverFor(nameof(TestFunctions.ReturnsPlainString));
        var transformer = CreateTransformer(resolver.Object);

        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);
        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.False(json.ContainsKey("outputSchema"));
    }

    [Fact]
    public void Transform_VoidReturnType_NoOutputSchema()
    {
        var resolver = CreateMethodResolverFor(nameof(TestFunctions.ReturnsVoid));
        var transformer = CreateTransformer(resolver.Object);

        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);
        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.False(json.ContainsKey("outputSchema"));
    }

    [Fact]
    public void Transform_TaskReturnType_NoOutputSchema()
    {
        var resolver = CreateMethodResolverFor(nameof(TestFunctions.ReturnsTask));
        var transformer = CreateTransformer(resolver.Object);

        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);
        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.False(json.ContainsKey("outputSchema"));
    }

    [Fact]
    public void Transform_MethodResolutionFails_NoOutputSchema()
    {
        MethodInfo? nullMethod = null;
        var resolver = new Mock<IFunctionMethodResolver>();
        resolver.Setup(r => r.TryResolveMethod(It.IsAny<IFunctionMetadata>(), out nullMethod))
            .Returns(false);

        var transformer = CreateTransformer(resolver.Object);

        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);
        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.False(json.ContainsKey("outputSchema"));
    }

    [Fact]
    public void Transform_AsyncNonMcpContentReturnType_NoOutputSchema()
    {
        var resolver = CreateMethodResolverFor(nameof(TestFunctions.ReturnsPlainObjectAsync));
        var transformer = CreateTransformer(resolver.Object);

        var fn = CreateFunctionMetadata("Func", ["{\"type\":\"mcpToolTrigger\",\"toolName\":\"MyTool\"}"]);
        transformer.Transform([fn.Object]);

        var json = JsonNode.Parse(fn.Object.RawBindings![0])!.AsObject();
        Assert.False(json.ContainsKey("outputSchema"));
    }

    #endregion

    #region UnwrapReturnType tests

    [Fact]
    public void UnwrapReturnType_Void_ReturnsNull()
    {
        Assert.Null(McpOutputSchemaTransformer.UnwrapReturnType(typeof(void)));
    }

    [Fact]
    public void UnwrapReturnType_Task_ReturnsNull()
    {
        Assert.Null(McpOutputSchemaTransformer.UnwrapReturnType(typeof(Task)));
    }

    [Fact]
    public void UnwrapReturnType_ValueTask_ReturnsNull()
    {
        Assert.Null(McpOutputSchemaTransformer.UnwrapReturnType(typeof(ValueTask)));
    }

    [Fact]
    public void UnwrapReturnType_TaskOfT_ReturnsT()
    {
        Assert.Equal(typeof(WeatherResult), McpOutputSchemaTransformer.UnwrapReturnType(typeof(Task<WeatherResult>)));
    }

    [Fact]
    public void UnwrapReturnType_ValueTaskOfT_ReturnsT()
    {
        Assert.Equal(typeof(WeatherResult), McpOutputSchemaTransformer.UnwrapReturnType(typeof(ValueTask<WeatherResult>)));
    }

    [Fact]
    public void UnwrapReturnType_PlainType_ReturnsSameType()
    {
        Assert.Equal(typeof(WeatherResult), McpOutputSchemaTransformer.UnwrapReturnType(typeof(WeatherResult)));
    }

    [Fact]
    public void UnwrapReturnType_String_ReturnsString()
    {
        Assert.Equal(typeof(string), McpOutputSchemaTransformer.UnwrapReturnType(typeof(string)));
    }

    #endregion

    #region Test types and functions

    [McpOutput]
    public class WeatherResult
    {
        [Description("Temperature in celsius")]
        public double Temperature { get; set; }

        [Description("Weather conditions description")]
        public required string Condition { get; set; }
    }

    [McpContent]
    public class ContentOnlyResult
    {
        public string? Status { get; set; }
    }

    public class PlainResult
    {
        public string? Message { get; set; }
    }

    internal class TestFunctions
    {
        public WeatherResult ReturnsWeatherResult() => new() { Condition = "Sunny" };

        public Task<WeatherResult> ReturnsWeatherResultAsync() => Task.FromResult(new WeatherResult { Condition = "Sunny" });

        public ValueTask<WeatherResult> ReturnsWeatherResultValueTask() => new(new WeatherResult { Condition = "Sunny" });

        public ContentOnlyResult ReturnsContentOnlyResult() => new();

        public string ReturnsPlainString() => "hello";

        public void ReturnsVoid() { }

        public Task ReturnsTask() => Task.CompletedTask;

        public Task<PlainResult> ReturnsPlainObjectAsync() => Task.FromResult(new PlainResult());
    }

    #endregion

    #region Test helpers

    internal class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> LogEntries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogEntries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }

        internal record LogEntry(LogLevel LogLevel, string Message);
    }

    #endregion
}
