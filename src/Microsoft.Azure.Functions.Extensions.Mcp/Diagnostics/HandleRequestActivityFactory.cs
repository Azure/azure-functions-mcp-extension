// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using ModelContextProtocol.Protocol.Messages;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

internal sealed class RequestActivityFactory
{
    private readonly ActivityHelper _activityHelper;
    private readonly List<IActivityTagProvider> _tagProviders = new();
    private static readonly ActivitySource _activitySource = new(TraceConstants.ExtensionActivitySource, TraceConstants.ExtensionActivitySourceVersion);

    public RequestActivityFactory()
    {
        _activityHelper = new ActivityHelper(_activitySource);

        // Register default tag providers
        RegisterTagProvider(new RequestInfoTagProviderV1());
    }

    public void RegisterTagProvider(IActivityTagProvider provider)
    {
        _tagProviders.Add(provider);
    }

    public Activity? CreateActivity(string name, JsonRpcRequest request)
    {
        // The use of ActivityContext with ActivityTraceId.CreateRandom() is intentional; otherwise, all tool calls would be correlated to the GET /runtime/webhooks/mcp/sse request.
        var rootContext = new ActivityContext(
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.None);

        return _activityHelper.StartServerActivity(name,
            request, rootContext,
            activity =>
            {
                foreach (var provider in _tagProviders)
                {
                    provider.AddTags(activity, request);
                }
            });
    }

    internal interface IActivityTagProvider
    {
        void AddTags(Activity activity, object context);
    }

    internal class RequestInfoTagProviderV1 : IActivityTagProvider
    {
        public void AddTags(Activity activity, object context)
        {
            if (context is JsonRpcRequest request)
            {
                activity.SetTag("request.id", request.Id);
                activity.SetTag("request.method", request.Method);
                activity.SetTag("type", "other");
            }
        }
    }
}
