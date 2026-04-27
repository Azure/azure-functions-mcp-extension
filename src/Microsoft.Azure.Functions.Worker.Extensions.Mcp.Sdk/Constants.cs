// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk;

internal static class Constants
{
    public const string ToolInvocationContextKey = "ToolInvocationContext";
    public const string PromptInvocationContextKey = "PromptInvocationContext";

    // Tool result content types
    public const string CallToolResultType = "call_tool_result";
    public const string MultiContentResult = "multi_content_result";
    public const string TextContextResult = "text";

    // Prompt result content types
    public const string GetPromptResultType = "get_prompt_result";
    public const string PromptMessagesType = "prompt_messages";

    // Binding JSON property keys
    public const string BindingType = "type";
    public const string BindingDirectionProperty = "direction";
    public const string BindingDirectionOut = "out";
}
