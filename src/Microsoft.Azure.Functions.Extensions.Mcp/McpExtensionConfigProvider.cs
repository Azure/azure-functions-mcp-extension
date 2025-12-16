// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.Functions.Extensions.Mcp.Backplane;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Microsoft.Azure.Functions.Extensions.Mcp;

[Extension("Mcp", "mcp")]
internal sealed class McpExtensionConfigProvider(IToolRegistry toolRegistry, IMcpRequestHandler requestHandler, IWebHookProvider webHookProvider, IMcpBackplaneService backplaneService, ILoggerFactory loggerFactory)
    : IExtensionConfigProvider, IAsyncConverter<HttpRequestMessage, HttpResponseMessage>, IDisposable
{
    private Func<Uri>? _webhookDelegate;

    public void Initialize(ExtensionConfigContext context)
    {
        var uri = context.GetWebhookHandler();

        var consoleLogger = loggerFactory.CreateLogger("Host.Function.Console");
        var extensionUri = uri?.GetLeftPart(UriPartial.Path) ?? string.Empty;
        consoleLogger.LogInformation("MCP server endpoint: {uri}", extensionUri);
        consoleLogger.LogInformation("MCP server legacy SSE endpoint: {uri}/sse", extensionUri);

        _webhookDelegate = () => webHookProvider.GetUrl(this);

        context.AddBindingRule<McpToolTriggerAttribute>()
            .BindToTrigger(new McpToolTriggerBindingProvider(toolRegistry));

        context.AddBindingRule<McpToolPropertyAttribute>()
            .Bind(new McpToolPropertyBindingProvider());

       backplaneService.StartAsync(CancellationToken.None)
           .GetAwaiter()
           .GetResult();
    }

    public async Task<HttpResponseMessage> ConvertAsync(HttpRequestMessage input, CancellationToken cancellationToken)
    {
        if (!input.Options.TryGetValue(new HttpRequestOptionsKey<HttpContext>(nameof(HttpContext)), out var context))
        {
            throw new InvalidOperationException("HttpContext not found in request options.");
        }

        await requestHandler.HandleRequest(context);

        var responseFeature = context.Features.Get<IHttpResponseFeature>();

        if (responseFeature != null)
        {
            context.Features.Set<IHttpResponseFeature>(new EmptyResponseFeature(responseFeature));
        }

        return context.GetHttpRequestMessage().CreateResponse();
    }

    public void Dispose()
    {
        backplaneService.StopAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        loggerFactory.Dispose();
    }

    private class EmptyResponseFeature(IHttpResponseFeature responseFeature) : IHttpResponseFeature
    {
        public int StatusCode { get => responseFeature.StatusCode; set { } }

        public Stream Body { get => responseFeature.Body; set { } }

        public bool HasStarted => responseFeature.HasStarted;

        public string? ReasonPhrase { get => responseFeature.ReasonPhrase; set { } }

        public IHeaderDictionary Headers { get => new HeaderDictionary(); set { } }

        public void OnCompleted(Func<object, Task> callback, object state) => responseFeature.OnCompleted(callback, state);

        public void OnStarting(Func<object, Task> callback, object state) => responseFeature.OnStarting(callback, state);
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
