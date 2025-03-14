using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

[Extension("Mcp")]
internal class McpExtensionConfigProvider : IExtensionConfigProvider, IAsyncConverter<HttpRequestMessage, HttpResponseMessage>
{
#pragma warning disable CS0618 // Type or member is obsolete
    private readonly IWebHookProvider _webHookProvider;
#pragma warning restore CS0618 // Type or member is obsolete

#pragma warning disable CS0618 // Type or member is obsolete
    public McpExtensionConfigProvider(IWebHookProvider webHookProvider)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        
        _webHookProvider = webHookProvider;
        Func<Uri> webhookDelegate = () => webHookProvider.GetUrl(this);
    }
    public void Initialize(ExtensionConfigContext context)
    {

        context.GetWebhookHandler();
        context.AddBindingRule<McpTriggerAttribute>()
            .BindToTrigger(new McpTriggerBindingProvider());
    }

    public async Task<HttpResponseMessage> ConvertAsync(HttpRequestMessage input, CancellationToken cancellationToken)
    {
        if (!input.Options.TryGetValue(new HttpRequestOptionsKey<HttpContext>(nameof(HttpContext)), out var context))
        {
            throw new InvalidOperationException("HttpContext not found in request options.");
        }

        // Set the appropriate headers for SSE.
        context.Response.Headers.Add("Content-Type", "text/event-stream");
        context.Response.Headers.Add("Cache-Control", "no-cache");
        context.Response.Headers.Add("Connection", "keep-alive");

        // Keep sending data as long as the client is connected.
        var counter = 0;

        while (!context.RequestAborted.IsCancellationRequested)
        {
            counter++;
            // Format the SSE data. Each event is separated by two newlines.
            await context.Response.WriteAsync($"data: Server message {counter}\n\n", cancellationToken: cancellationToken);

            // Flush the data to ensure it gets sent to the client immediately.
            await context.Response.Body.FlushAsync(cancellationToken);

            // Wait for 1 second before sending the next event.
            await Task.Delay(1000, cancellationToken);
        }

        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}