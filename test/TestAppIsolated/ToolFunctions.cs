using System.ComponentModel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using static TestAppIsolated.ToolsInformation;

namespace TestAppIsolated;

public class TestFunction
{
    private readonly ILogger<TestFunction> _logger;

    public TestFunction(ILogger<TestFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(GetSnippet))]
    public object GetSnippet(
        [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)] ToolInvocationContext context,
        [McpToolProperty(SnippetNamePropertyName, StringPropertyType, SnippetNamePropertyDescription, true)] string name)
    {
        return SnippetsCache.Snippets.TryGetValue(name, out var snippet)
            ? snippet
            : string.Empty;
    }

    [Function(nameof(SaveSnippet))]
    public void SaveSnippet([McpToolTrigger(SaveSnippetToolName, SaveSnippetToolDescription)] ToolInvocationContext context, Snippet snippet)
    {
        Console.WriteLine($"Function name: {context.Name}");
        SnippetsCache.Snippets[snippet.Name] = snippet.Content ?? string.Empty;
    }

    [Function(nameof(SearchSnippets))]
    public object SearchSnippets([McpToolTrigger(SearchSnippetsToolName, SearchSnippetsToolDescription)] SnippetSearchRequest searchRequest)
    {
        var comparisonType = searchRequest.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return SnippetsCache.Snippets
            .Where(kvp => kvp.Key.Contains(searchRequest.Pattern, comparisonType))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public class Snippet
    {
        [Description("The name of the snippet")]
        public required string Name { get; set; }

        [Description("The content of the snippet")]
        public string? Content { get; set; }
    }

    public class SnippetSearchRequest
    {
        [Description("Pattern to search for")]
        public required string Pattern { get; set; }

        [Description("Whether search is case sensitive")]
        public bool CaseSensitive { get; set; }
    }
}
