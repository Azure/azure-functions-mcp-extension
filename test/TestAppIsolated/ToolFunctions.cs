using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using static TestAppIsolated.ToolsInformation;

namespace TestAppIsolated;

public class TestFunction
{
    [Function(nameof(GetSnippet))]
    public object GetSnippet(
        [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)] ToolInvocationContext context,
        [McpToolProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription, true)] string name)
    {
        return SnippetsCache.Snippets.TryGetValue(name, out var snippet)
            ? snippet
            : string.Empty;
    }

    // [Function(nameof(SaveSnippet))]
    // public void SaveSnippet(
    //     [McpToolTrigger(SaveSnippetToolName, SaveSnippetToolDescription)] ToolInvocationContext context,
    //     [McpToolProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription, true)] string name,
    //     [McpToolProperty(SnippetPropertyName, PropertyType, SnippetPropertyDescription)] string snippet)
    // {
    //     SnippetsCache.Snippets[name] = snippet;
    // }

    [Function(nameof(SearchSnippets))]
    public object SearchSnippets(
        [McpToolTrigger("searchSnippets", "Search for snippets by name pattern")] ToolInvocationContext context,
        [McpToolProperty("pattern", PropertyType, "Pattern to search for", true)] string pattern,
        [McpToolProperty("caseSensitive", "boolean", "Whether search is case sensitive", false)] bool caseSensitive)
    {
        var comparisonType = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return SnippetsCache.Snippets
            .Where(kvp => kvp.Key.Contains(pattern, comparisonType))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    [Function(nameof(SaveSnippetPoco))]
    public void SaveSnippetPoco(
        [McpToolTrigger(SaveSnippetToolName, SaveSnippetToolDescription)] ToolInvocationContext context,
        [McpToolProperty("snippet", "object", "Snippet object containing snippet name and content", true)] Snippet snippet)
    {
        SnippetsCache.Snippets[snippet.Name] = snippet.Content;
    }

    public class Snippet
    {
        public string Name { get; set; }
        public string Content { get; set; }
    }
}
