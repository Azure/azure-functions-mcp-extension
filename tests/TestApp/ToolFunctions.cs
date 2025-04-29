using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.WebJobs;
using ModelContextProtocol.Protocol.Types;
using static TestApp.ToolsInformation;

namespace TestApp;

public class TestFunction
{
    [FunctionName(nameof(GetSnippet))]
    public object GetSnippet(
        [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)]
        string context,
        [McpToolProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription)]
        string name)
    {
        return SnippetsCache.Snippets.TryGetValue(name, out var snippet)
            ? snippet
            : string.Empty;
    }

    [FunctionName(nameof(SaveSnippet))]
    public void SaveSnippet(
        [McpToolTrigger(SaveSnippetToolName, SaveSnippetToolDescription)]
        CallToolRequestParams context,
        [McpToolProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription)]
        string name,
        [McpToolProperty(SnippetPropertyName, PropertyType, SnippetPropertyDescription)]
        string snippet)
    {
        SnippetsCache.Snippets[name] = snippet;
    }
}