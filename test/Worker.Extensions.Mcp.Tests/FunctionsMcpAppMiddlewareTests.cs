// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.McpApp;
using Microsoft.Extensions.Options;
using Moq;

namespace Worker.Extensions.Mcp.Tests;

public class FunctionsMcpAppMiddlewareTests : IDisposable
{
    public FunctionsMcpAppMiddlewareTests()
    {
        McpAppUtilities.ClearForTesting();
    }

    public void Dispose()
    {
        McpAppUtilities.ClearForTesting();
    }

    [Fact]
    public async Task Invoke_NonSyntheticFunction_CallsNext()
    {
        var middleware = CreateMiddleware();
        var context = CreateContext("RegularFunction");

        var nextCalled = false;
        await middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_SyntheticFunction_WithoutResourceContext_CallsNext()
    {
        RegisterSyntheticTool("myTool");
        var middleware = CreateMiddleware("myTool");

        // No ResourceInvocationContextKey in Items
        var context = CreateContext(
            McpAppUtilities.SyntheticFunctionName("myTool"),
            includeResourceContext: false);

        var nextCalled = false;
        await middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_SyntheticFunction_NullAppOptions_CallsNext()
    {
        RegisterSyntheticTool("noApp");

        // ToolOptions with null AppOptions
        var middleware = CreateMiddleware("noApp", appOptions: null);
        var context = CreateContext(
            McpAppUtilities.SyntheticFunctionName("noApp"),
            includeResourceContext: true);

        var nextCalled = false;
        await middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_SyntheticFunction_NoViews_CallsNext()
    {
        RegisterSyntheticTool("emptyViews");

        var appOptions = new AppOptions(); // No views added
        var middleware = CreateMiddleware("emptyViews", appOptions);
        var context = CreateContext(
            McpAppUtilities.SyntheticFunctionName("emptyViews"),
            includeResourceContext: true);

        var nextCalled = false;
        await middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_SyntheticFunction_WithView_ShortCircuits()
    {
        RegisterSyntheticTool("myApp");

        var appOptions = new AppOptions();
#pragma warning disable MCP001
        appOptions.Views[string.Empty] = new ViewOptions
        {
            Source = McpViewSourceTestExtensions.FromHtml("<html>Hello</html>")
        };
#pragma warning restore MCP001

        var middleware = CreateMiddleware("myApp", appOptions);
        var (context, resultCapture) = CreateContextWithResultCapture(
            McpAppUtilities.SyntheticFunctionName("myApp"),
            includeResourceContext: true);

        var nextCalled = false;
        await middleware.Invoke(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.False(nextCalled);
        Assert.NotNull(resultCapture.Value);
    }

    [Fact]
    public async Task Invoke_SyntheticFunction_ResultContainsHtml()
    {
        RegisterSyntheticTool("htmlApp");

        var appOptions = new AppOptions();
#pragma warning disable MCP001
        appOptions.Views[string.Empty] = new ViewOptions
        {
            Source = McpViewSourceTestExtensions.FromHtml("<html><body>Test Content</body></html>")
        };
#pragma warning restore MCP001

        var middleware = CreateMiddleware("htmlApp", appOptions);
        var (context, resultCapture) = CreateContextWithResultCapture(
            McpAppUtilities.SyntheticFunctionName("htmlApp"),
            includeResourceContext: true);

        await middleware.Invoke(context, _ => Task.CompletedTask);

        var resultJson = resultCapture.Value as string;
        Assert.NotNull(resultJson);

        // The result is double-serialized: McpAppResourceResult { content: "<json string>" }
        var resourceResult = JsonNode.Parse(resultJson!)!.AsObject();
        Assert.True(resourceResult.ContainsKey("content"));

        var contentJson = resourceResult["content"]!.GetValue<string>();
        var content = JsonNode.Parse(contentJson)!.AsObject();
        Assert.Contains("Test Content", content["text"]!.GetValue<string>());
        Assert.Equal("text/html;profile=mcp-app", content["mimeType"]!.GetValue<string>());
    }

    [Fact]
    public async Task Invoke_SyntheticFunction_ResultContainsResourceUri()
    {
        RegisterSyntheticTool("uriApp");

        var appOptions = new AppOptions();
#pragma warning disable MCP001
        appOptions.Views[string.Empty] = new ViewOptions
        {
            Source = McpViewSourceTestExtensions.FromHtml("<html/>")
        };
#pragma warning restore MCP001

        var middleware = CreateMiddleware("uriApp", appOptions);
        var (context, resultCapture) = CreateContextWithResultCapture(
            McpAppUtilities.SyntheticFunctionName("uriApp"),
            includeResourceContext: true);

        await middleware.Invoke(context, _ => Task.CompletedTask);

        var resultJson = resultCapture.Value as string;
        var resourceResult = JsonNode.Parse(resultJson!)!.AsObject();
        var contentJson = resourceResult["content"]!.GetValue<string>();
        var content = JsonNode.Parse(contentJson)!.AsObject();

        Assert.Equal("ui://uriApp/view", content["uri"]!.GetValue<string>());
    }

    [Fact]
    public async Task Invoke_SyntheticFunction_WithMetadata_IncludesMetaUi()
    {
        RegisterSyntheticTool("metaApp");

        var appOptions = new AppOptions();
#pragma warning disable MCP001
        appOptions.Views[string.Empty] = new ViewOptions
        {
            Source = McpViewSourceTestExtensions.FromHtml("<html/>"),
            PrefersBorder = true,
            Domain = "app.example.com",
            Permissions = McpAppPermissions.ClipboardWrite,
            Csp = new CspOptions()
        };
#pragma warning restore MCP001
        appOptions.Views[string.Empty].Csp!.ConnectDomains.Add("https://api.example.com");

        var middleware = CreateMiddleware("metaApp", appOptions);
        var (context, resultCapture) = CreateContextWithResultCapture(
            McpAppUtilities.SyntheticFunctionName("metaApp"),
            includeResourceContext: true);

        await middleware.Invoke(context, _ => Task.CompletedTask);

        var resultJson = resultCapture.Value as string;
        var resourceResult = JsonNode.Parse(resultJson!)!.AsObject();
        var contentJson = resourceResult["content"]!.GetValue<string>();
        var content = JsonNode.Parse(contentJson)!.AsObject();

        Assert.True(content.ContainsKey("_meta"));
        var meta = content["_meta"]!.AsObject();
        var ui = meta["ui"]!.AsObject();

        Assert.True(ui["prefersBorder"]!.GetValue<bool>());
        Assert.Equal("app.example.com", ui["domain"]!.GetValue<string>());
        Assert.True(ui["permissions"]!.AsObject().ContainsKey("clipboardWrite"));
        Assert.Equal("https://api.example.com",
            ui["csp"]!.AsObject()["connectDomains"]!.AsArray()[0]!.GetValue<string>());
    }

    [Fact]
    public async Task Invoke_SyntheticFunction_NoMetadata_OmitsMetaUi()
    {
        RegisterSyntheticTool("plainApp");

        var appOptions = new AppOptions();
#pragma warning disable MCP001
        appOptions.Views[string.Empty] = new ViewOptions
        {
            Source = McpViewSourceTestExtensions.FromHtml("<html/>")
        };
#pragma warning restore MCP001

        var middleware = CreateMiddleware("plainApp", appOptions);
        var (context, resultCapture) = CreateContextWithResultCapture(
            McpAppUtilities.SyntheticFunctionName("plainApp"),
            includeResourceContext: true);

        await middleware.Invoke(context, _ => Task.CompletedTask);

        var resultJson = resultCapture.Value as string;
        var resourceResult = JsonNode.Parse(resultJson!)!.AsObject();
        var contentJson = resourceResult["content"]!.GetValue<string>();
        var content = JsonNode.Parse(contentJson)!.AsObject();

        Assert.False(content.ContainsKey("_meta"));
    }

    [Fact]
    public async Task Invoke_SyntheticFunction_FallsBackToFirstView()
    {
        RegisterSyntheticTool("fallbackApp");

        var appOptions = new AppOptions();
#pragma warning disable MCP001
        // No default (empty key) view — only a named view
        appOptions.Views["dashboard"] = new ViewOptions
        {
            Source = McpViewSourceTestExtensions.FromHtml("<html>Dashboard</html>")
        };
#pragma warning restore MCP001

        var middleware = CreateMiddleware("fallbackApp", appOptions);
        var (context, resultCapture) = CreateContextWithResultCapture(
            McpAppUtilities.SyntheticFunctionName("fallbackApp"),
            includeResourceContext: true);

        await middleware.Invoke(context, _ => Task.CompletedTask);

        var resultJson = resultCapture.Value as string;
        Assert.NotNull(resultJson);

        var resourceResult = JsonNode.Parse(resultJson!)!.AsObject();
        var contentJson = resourceResult["content"]!.GetValue<string>();
        var content = JsonNode.Parse(contentJson)!.AsObject();
        Assert.Contains("Dashboard", content["text"]!.GetValue<string>());
    }

    // --- Helpers ---

    private static void RegisterSyntheticTool(string toolName)
    {
        McpAppUtilities.Register(McpAppUtilities.SyntheticFunctionName(toolName));
    }

    private static FunctionsMcpAppMiddleware CreateMiddleware(
        string? toolName = null,
        AppOptions? appOptions = null)
    {
        var toolOptions = new ToolOptions
        {
            Properties = [],
            AppOptions = appOptions
        };

        var optionsMonitor = new Mock<IOptionsMonitor<ToolOptions>>();
        optionsMonitor
            .Setup(o => o.Get(It.IsAny<string>()))
            .Returns(toolOptions);

        if (toolName is not null)
        {
            optionsMonitor
                .Setup(o => o.Get(toolName))
                .Returns(toolOptions);
        }

        return new FunctionsMcpAppMiddleware(optionsMonitor.Object);
    }

    private static FunctionContext CreateContext(
        string functionName,
        bool includeResourceContext = false)
    {
        var items = new Dictionary<object, object?>();
        if (includeResourceContext)
        {
            items[Constants.ResourceInvocationContextKey] =
                new ResourceInvocationContext($"ui://{functionName}/view");
        }

        var functionDefinitionMock = new Mock<FunctionDefinition>();
        functionDefinitionMock.SetupGet(d => d.Name).Returns(functionName);
        functionDefinitionMock.SetupGet(d => d.InputBindings)
            .Returns(ImmutableDictionary<string, BindingMetadata>.Empty);

        var contextMock = new Mock<FunctionContext>();
        contextMock.SetupGet(c => c.FunctionDefinition).Returns(functionDefinitionMock.Object);
        contextMock.SetupGet(c => c.Items).Returns(items!);
        contextMock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        return contextMock.Object;
    }

    private static (FunctionContext context, InvocationResultCapture capture) CreateContextWithResultCapture(
        string functionName,
        bool includeResourceContext = false)
    {
        var capture = new InvocationResultCapture();

        var items = new Dictionary<object, object?>();
        if (includeResourceContext)
        {
            items[Constants.ResourceInvocationContextKey] =
                new ResourceInvocationContext($"ui://{functionName}/view");
        }

        var functionDefinitionMock = new Mock<FunctionDefinition>();
        functionDefinitionMock.SetupGet(d => d.Name).Returns(functionName);
        functionDefinitionMock.SetupGet(d => d.InputBindings)
            .Returns(ImmutableDictionary<string, BindingMetadata>.Empty);

        // Create a features collection with IFunctionBindingsFeature to support GetInvocationResult()
        var features = new TestInvocationFeatures(capture);

        var contextMock = new Mock<FunctionContext>();
        contextMock.SetupGet(c => c.FunctionDefinition).Returns(functionDefinitionMock.Object);
        contextMock.SetupGet(c => c.Items).Returns(items!);
        contextMock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        contextMock.SetupGet(c => c.Features).Returns(features);

        return (contextMock.Object, capture);
    }

    internal class InvocationResultCapture
    {
        internal object? InternalValue { get; set; }
        public object? Value => InternalValue;
    }

    /// <summary>
    /// Minimal IInvocationFeatures implementation that provides IFunctionBindingsFeature
    /// for testing middleware that calls context.GetInvocationResult().
    /// Uses DispatchProxy to implement the internal IFunctionBindingsFeature interface.
    /// </summary>
    private class TestInvocationFeatures : IInvocationFeatures
    {
        private readonly Dictionary<Type, object> _features = new();

        public TestInvocationFeatures(InvocationResultCapture capture)
        {
            // IFunctionBindingsFeature is internal. Use DispatchProxy to create
            // a runtime implementation that stores InvocationResult in our capture.
            var featureType = typeof(FunctionContext).Assembly
                .GetType("Microsoft.Azure.Functions.Worker.Context.Features.IFunctionBindingsFeature")!;

            // DispatchProxy.Create requires both type args. Use reflection since
            // one is an internal type.
            var createMethod = typeof(DispatchProxy)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "Create" && m.GetGenericArguments().Length == 2)
                .MakeGenericMethod(featureType, typeof(BindingsFeatureProxy));

            var proxy = createMethod.Invoke(null, null)!;
            ((BindingsFeatureProxy)proxy).Capture = capture;

            _features[featureType] = proxy;
        }

        public T? Get<T>() =>
            _features.TryGetValue(typeof(T), out var feature) ? (T)feature : default;

        public void Set<T>(T instance) =>
            _features[typeof(T)] = instance!;

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() =>
            _features.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }

    /// <summary>
    /// DispatchProxy that intercepts IFunctionBindingsFeature property calls,
    /// storing InvocationResult in the capture object.
    /// </summary>
    public class BindingsFeatureProxy : DispatchProxy
    {
        internal InvocationResultCapture? Capture { get; set; }

        protected override object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null)
            {
                return null;
            }

            // Handle InvocationResult getter
            if (targetMethod.Name == "get_InvocationResult")
            {
                return Capture?.InternalValue;
            }

            // Handle InvocationResult setter
            if (targetMethod.Name == "set_InvocationResult")
            {
                if (Capture is not null)
                {
                    Capture.InternalValue = args?[0];
                }

                return null;
            }

            // Return defaults for other properties
            return targetMethod.ReturnType.IsValueType
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }
}
