// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using ModelContextProtocol.Protocol;
using static Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics.TraceConstants;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

internal sealed class RequestActivityFactory
{
    private readonly ActivityHelper _activityHelper;
    private readonly List<IActivityTagProvider> _tagProviders = new();
    private static readonly ActivitySource _activitySource = new(ExtensionActivitySource, ExtensionActivitySourceVersion);

    public RequestActivityFactory()
    {
        _activityHelper = new ActivityHelper(_activitySource);

        // Register default tag providers following OTel MCP semantic conventions
        RegisterTagProvider(new McpSemanticConventionTagProvider());
    }

    public void RegisterTagProvider(IActivityTagProvider provider)
    {
        _tagProviders.Add(provider);
    }

    public Activity? CreateActivity(string name, JsonRpcRequest request, string? sessionId = null, string? protocolVersion = null)
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
                    provider.AddTags(activity, new McpRequestContext(request, sessionId, protocolVersion));
                }
            });
    }

    internal interface IActivityTagProvider
    {
        void AddTags(Activity activity, object context);
    }

    /// <summary>
    /// Context object for MCP request instrumentation.
    /// </summary>
    internal sealed record McpRequestContext(JsonRpcRequest Request, string? SessionId, string? ProtocolVersion);

    /// <summary>
    /// Tag provider implementing OTel MCP semantic conventions.
    /// See: https://opentelemetry.io/docs/specs/semconv/gen-ai/mcp/
    /// </summary>
    internal class McpSemanticConventionTagProvider : IActivityTagProvider
    {
        public void AddTags(Activity activity, object context)
        {
            if (context is not McpRequestContext mcpContext)
            {
                return;
            }

            var request = mcpContext.Request;

            // Required: mcp.method.name
            activity.SetTag(McpAttributes.MethodName, request.Method);

            // Conditionally Required: jsonrpc.request.id (when client executes a request)
            activity.SetTag(McpAttributes.JsonRpcRequestId, request.Id.ToString());

            // Recommended: mcp.session.id
            if (!string.IsNullOrEmpty(mcpContext.SessionId))
            {
                activity.SetTag(McpAttributes.SessionId, mcpContext.SessionId);
            }

            // Recommended: mcp.protocol.version
            if (!string.IsNullOrEmpty(mcpContext.ProtocolVersion))
            {
                activity.SetTag(McpAttributes.ProtocolVersion, mcpContext.ProtocolVersion);
            }

            // Add method-specific attributes
            AddMethodSpecificTags(activity, request);
        }

        private static void AddMethodSpecificTags(Activity activity, JsonRpcRequest request)
        {
            switch (request.Method)
            {
                case McpMethods.ToolsCall:
                    // Recommended: gen_ai.operation.name
                    activity.SetTag(McpAttributes.OperationName, GenAiOperations.ExecuteTool);

                    // Conditionally Required: gen_ai.tool.name
                    if (TryGetToolName(request, out var toolName))
                    {
                        activity.SetTag(McpAttributes.ToolName, toolName);
                    }
                    break;

                case McpMethods.ResourcesRead:
                    // Conditionally Required: mcp.resource.uri
                    if (TryGetResourceUri(request, out var resourceUri))
                    {
                        activity.SetTag(McpAttributes.ResourceUri, resourceUri);
                    }
                    break;

                case McpMethods.PromptsGet:
                    // Conditionally Required: gen_ai.prompt.name
                    if (TryGetPromptName(request, out var promptName))
                    {
                        activity.SetTag(McpAttributes.PromptName, promptName);
                    }
                    break;
            }
        }

        private static bool TryGetToolName(JsonRpcRequest request, out string? toolName)
        {
            toolName = null;

            if (request.Params is System.Text.Json.Nodes.JsonObject paramsObject
                && paramsObject.TryGetPropertyValue("name", out var nameNode)
                && nameNode is System.Text.Json.Nodes.JsonValue nameValue)
            {
                toolName = nameValue.ToString();
                return !string.IsNullOrEmpty(toolName);
            }

            return false;
        }

        private static bool TryGetResourceUri(JsonRpcRequest request, out string? resourceUri)
        {
            resourceUri = null;

            if (request.Params is System.Text.Json.Nodes.JsonObject paramsObject
                && paramsObject.TryGetPropertyValue("uri", out var uriNode)
                && uriNode is System.Text.Json.Nodes.JsonValue uriValue)
            {
                resourceUri = uriValue.ToString();
                return !string.IsNullOrEmpty(resourceUri);
            }

            return false;
        }

        private static bool TryGetPromptName(JsonRpcRequest request, out string? promptName)
        {
            promptName = null;

            if (request.Params is System.Text.Json.Nodes.JsonObject paramsObject
                && paramsObject.TryGetPropertyValue("name", out var nameNode)
                && nameNode is System.Text.Json.Nodes.JsonValue nameValue)
            {
                promptName = nameValue.ToString();
                return !string.IsNullOrEmpty(promptName);
            }

            return false;
        }
    }
}
