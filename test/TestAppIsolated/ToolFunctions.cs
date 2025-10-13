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

    [Function(nameof(HappyFunction))]
    public string HappyFunction(
        [McpToolTrigger(nameof(HappyFunction), "Responds to the user with a hello message.")] ToolInvocationContext context,
        [McpToolProperty(nameof(name), "The name of the person to greet.")] string name,
        [McpToolProperty(nameof(job), "The job of the person.")] string? job,
        [McpToolProperty("attributes", "Attributes of the person.")] IEnumerable<string> attributes,
        [McpToolProperty("numbers", "Attributes of the person.")] IEnumerable<int> numbers,
        [McpToolProperty(nameof(age), "The age of the person.")] int age = 20,
        [McpToolProperty(nameof(isHappy), "The happiness of the person.")] bool isHappy = true)
    {
        _logger.LogInformation("C# MCP tool trigger function processed a request: {ToolName}", context.Name);
        var entityToGreet = name ?? "world";
        return $"""
            Hello, {entityToGreet}! Job: {job ?? "Unemployed"} | Age: {age} | Happy? {isHappy}.
            Attributes: {(attributes.Any() ? string.Join(", ", attributes) : "(none)")}
            """;
    }

    [Function(nameof(BirthdayTracker))]
    public string BirthdayTracker(
        [McpToolTrigger(nameof(BirthdayTracker), "Capture user birthday information.")] ToolInvocationContext context,
        [McpToolProperty(nameof(userId), "User ID")] Guid userId,
        [McpToolProperty(nameof(birthday), "Birthday")] DateTime birthday)
    {
        return $"Hello {userId}, {birthday}";
    }

    [Function(nameof(SingleArgumentFunction))]
    public string SingleArgumentFunction(
       [McpToolTrigger(nameof(SingleArgumentFunction), "Echoes a single argument passed into the tool.")] ToolInvocationContext context,
       [McpToolProperty(nameof(argument), "The name of the person to greet.")] string argument)
    {
        _logger.LogInformation("C# MCP tool trigger function processed a request: {ToolName}", context.Name);
        return argument ?? "(null argument)";
    }

    [Function(nameof(SingleArgumentWithDefaultFunction))]
    public string SingleArgumentWithDefaultFunction(
       [McpToolTrigger(nameof(SingleArgumentWithDefaultFunction), "Echoes a single argument passed into the tool.")] ToolInvocationContext context,
       [McpToolProperty(nameof(argument), "The name of the person to greet.")] string argument = "(no-argument)")
    {
        _logger.LogInformation("C# MCP tool trigger function processed a request: {ToolName}", context.Name);
        return argument;
    }

    [Function(nameof(GetSnippet))]
    public object GetSnippet(
        [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)] ToolInvocationContext context,
        [McpToolProperty(SnippetNamePropertyName, SnippetNamePropertyDescription, true)] string name)
    {
        return SnippetsCache.Snippets.TryGetValue(name, out var snippet)
            ? snippet
            : string.Empty;
    }

    [Function(nameof(SaveSnippet))]
    public void SaveSnippet(
        [McpToolTrigger(SaveSnippetToolName, SaveSnippetToolDescription)] Snippet snippet,
        ToolInvocationContext context)
    {
        _logger.LogInformation($"Tool name: {context.Name}");
        SnippetsCache.Snippets[snippet.Name] = snippet.Content ?? string.Empty;
    }

    [Function(nameof(SearchSnippets))]
    public object SearchSnippets(
        [McpToolTrigger(SearchSnippetsToolName, SearchSnippetsToolDescription)] SnippetSearchRequest searchRequest,
        ToolInvocationContext context)
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
