using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.WebJobs;
using ModelContextProtocol.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using static TestApp.ToolsInformation;

namespace TestApp;

public class TestFunction
{
    [FunctionName(nameof(GetSnippet))]
    public object GetSnippet(
        [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)]
        string context,
        [McpToolProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription, true)]
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
        [McpToolProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription, true)]
        string name,
        [McpToolProperty(SnippetPropertyName, PropertyType, SnippetPropertyDescription)]
        string snippet)
    {
        SnippetsCache.Snippets[name] = snippet;
    }

    [FunctionName(nameof(SearchSnippets))]
    public object SearchSnippets(
        [McpToolTrigger("searchSnippets", "Search for snippets by name pattern")]
        string context,
        [McpToolProperty("pattern", PropertyType, "Pattern to search for", true)]
        string pattern,
        [McpToolProperty("caseSensitive", "boolean", "Whether search is case sensitive", false)]
        bool caseSensitive)
    {
        var comparisonType = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return SnippetsCache.Snippets
            .Where(kvp => kvp.Key.Contains(pattern, comparisonType))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
