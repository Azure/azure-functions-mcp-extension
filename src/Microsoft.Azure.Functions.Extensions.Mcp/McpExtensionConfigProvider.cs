using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Microsoft.Azure.Functions.Extensions.Mcp;

[Extension("Mcp", "mcp")]
internal sealed class McpExtensionConfigProvider(IToolRegistry toolRegistry, IRequestHandler requestHandler, IWebHookProvider webHookProvider) : IExtensionConfigProvider, IAsyncConverter<HttpRequestMessage, HttpResponseMessage>
{
    private Func<Uri>? _webhookDelegate;

    public void Initialize(ExtensionConfigContext context)
    {
        var uri = context.GetWebhookHandler();

        _webhookDelegate = () => webHookProvider.GetUrl(this);

        context.AddBindingRule<McpToolTriggerAttribute>()
            .BindToTrigger(new McpTriggerBindingProvider(toolRegistry));

        context.AddBindingRule<McpToolPropertyAttribute>()
            .Bind(new McpToolPropertyBindingProvider());
    }

    public async Task<HttpResponseMessage> ConvertAsync(HttpRequestMessage input, CancellationToken cancellationToken)
    {
        if (!input.Options.TryGetValue(new HttpRequestOptionsKey<HttpContext>(nameof(HttpContext)), out var context))
        {
            throw new InvalidOperationException("HttpContext not found in request options.");
        }

        var isSse = context.Request.GetUri().AbsolutePath.Contains("sse");

        if (isSse)
        {
            await requestHandler.HandleSseRequest(context);
        }
        else
        {
            await requestHandler.HandleMessageRequest(context);
        }

        var responseFeature = context.Features.Get<IHttpResponseFeature>();

        if (responseFeature != null)
        {
            context.Features.Set<IHttpResponseFeature>(new EmptyResponseFeature(responseFeature));
        }

        return context.GetHttpRequestMessage().CreateResponse();
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
