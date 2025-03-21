using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Model;
using System.Collections.Generic;
using System;

namespace TestApp
{
    public class TestFunction
    {
        [FunctionName(nameof(GetSnippet))]
        public object GetSnippet(
            [McpToolTrigger("getsnippets", "Gets code snippets from your snippet collection.")] ToolInvocationContext context,
            [McpToolProperty("snippetname", "text",  "The name of the snippet.")] string name)
        {
            if (SnippetsCache.Snippets.TryGetValue(name, out var snippet))
            {
                return snippet;
            }

            return "// Snippet not found...";
        }

        [FunctionName(nameof(SaveSnippet))]
        public object SaveSnippet(
            [McpToolTrigger("savesnippet", "Saves a code snippet into your snippet collection.")] ToolInvocationContext context,
            [McpToolProperty("snippetname", "text", "The name of the snippet.")] string name,
            [McpToolProperty("snippet", "text", "The code snippet.")] string snippet)
        {
            SnippetsCache.Snippets[name] = snippet;
            return "Saved";
        }
    }
}
