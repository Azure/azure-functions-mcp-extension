using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Model;
using Microsoft.Azure.WebJobs;

namespace TestApp;

public class TestFunction
{
    [FunctionName(nameof(GetSnippet))]
    public object GetSnippet(
        [McpToolTrigger("getsnippets", "Gets code snippets from your snippet collection.")]
        string context,
        [McpToolProperty("snippetname", "text", "The name of the snippet.")]
        string name)
    {
        return SnippetsCache.Snippets.TryGetValue(name, out var snippet)
            ? snippet
            : string.Empty;
    }

    [FunctionName(nameof(SaveSnippet))]
    public void SaveSnippet(
        [McpToolTrigger("savesnippet", "Saves a code snippet into your snippet collection.")]
        ToolInvocationContext context,
        [McpToolProperty("snippetname", "text", "The name of the snippet.")]
        string name,
        [McpToolProperty("snippet", "text", "The code snippet.")]
        string snippet)
    {
        SnippetsCache.Snippets[name] = snippet;
    }
}