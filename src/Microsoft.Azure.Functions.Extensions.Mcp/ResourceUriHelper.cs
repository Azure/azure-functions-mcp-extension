// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Helper methods for validating and inspecting MCP resource URIs, including templated URIs.
/// </summary>
internal static partial class ResourceUriHelper
{
    [GeneratedRegex(@"\{[^{}]+\}", RegexOptions.CultureInvariant)]
    private static partial Regex TemplateExpressionMatcher();

    [GeneratedRegex("[^A-Za-z0-9_]", RegexOptions.CultureInvariant)]
    private static partial Regex SanitizeCharPattern();

    /// <summary>
    /// Determines whether the URI contains template expressions.
    /// </summary>
    public static bool IsTemplate(string uri) => TemplateExpressionMatcher().IsMatch(uri);

    /// <summary>
    /// Normalizes a template URI by replacing all template expressions with a common placeholder.
    /// Used to detect structurally equivalent templates.
    /// </summary>
    internal static string NormalizeTemplateStructure(string uriTemplate) =>
        TemplateExpressionMatcher().Replace(uriTemplate, "{_}");

    /// <summary>
    /// Returns the ordered list of parameter names declared in a URI template.
    /// </summary>
    public static IReadOnlyList<string> GetTemplateParameterNames(string uriTemplate)
    {
        ArgumentNullException.ThrowIfNull(uriTemplate);

        return [.. TemplateExpressionMatcher().Matches(uriTemplate).Select(m => m.Value[1..^1])];
    }

    /// <summary>
    /// Builds a regex that matches the supplied URI template and exposes named capture groups for each parameter.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the template has duplicate or colliding parameter names.</exception>
    public static Regex BuildTemplateRegex(string uriTemplate)
    {
        ArgumentNullException.ThrowIfNull(uriTemplate);

        if (!IsTemplate(uriTemplate))
        {
            throw new ArgumentException("Resource URI must contain at least one template expression.", nameof(uriTemplate));
        }

        ValidateTemplateSyntax(uriTemplate);
        var parameters = GetTemplateParameterNames(uriTemplate);
        ValidateParameters(parameters, uriTemplate);

        return BuildRegexCore(uriTemplate);
    }

    /// <summary>
    /// Sanitizes a parameter name for use in a regex named capture group.
    /// </summary>
    private static string SanitizeParameterName(string parameter) =>
        SanitizeCharPattern().Replace(parameter, "_");

    /// <summary>
    /// Builds a mapping from sanitized regex group names back to the original parameter names.
    /// </summary>
    private static Dictionary<string, string> BuildSanitizedToOriginalMap(IReadOnlyList<string> parameters)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in parameters)
        {
            map[SanitizeParameterName(p)] = p;
        }

        return map;
    }

    /// <summary>
    /// Attempts to extract template parameter values from an actual URI using the provided template.
    /// </summary>
    public static bool TryExtractParameters(string uriTemplate, string actualUri, [NotNullWhen(true)] out IReadOnlyDictionary<string, string>? values)
    {
        values = null;

        if (!IsTemplate(uriTemplate))
        {
            return false;
        }

        var regex = BuildTemplateRegex(uriTemplate);
        var match = regex.Match(actualUri);
        if (!match.Success)
        {
            return false;
        }

        var parameters = GetTemplateParameterNames(uriTemplate);
        var sanitizedMap = BuildSanitizedToOriginalMap(parameters);

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var groupName in regex.GetGroupNames())
        {
            if (int.TryParse(groupName, out _))
            {
                continue; // skip numeric groups
            }

            // Map sanitized regex group name back to original parameter name
            var originalName = sanitizedMap.TryGetValue(groupName, out var orig)
                ? orig
                : groupName;
            dict[originalName] = match.Groups[groupName].Value;
        }

        values = dict;
        return true;
    }

    /// <summary>
    /// Validates a resource URI or URI template per MCP requirements.
    /// - Must not be null/whitespace
    /// - Must include a scheme and be absolute after substituting template parameters
    /// - Template expressions must be balanced and non-empty
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void Validate(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("Resource URI cannot be null or whitespace.", nameof(uri));
        }

        var isTemplate = IsTemplate(uri);

        if (isTemplate)
        {
            // Basic brace balance check for templates.
            if (uri.Count(c => c == '{') != uri.Count(c => c == '}'))
            {
                throw new ArgumentException("Resource URI template has unbalanced braces.", nameof(uri));
            }

            // Validate template syntax and parameters (adjacent expressions, collisions, etc.)
            ValidateTemplateSyntax(uri);
            var parameters = GetTemplateParameterNames(uri);
            ValidateParameters(parameters, uri);
        }
        else if (uri.Contains('{') || uri.Contains('}'))
        {
            // URIs like "user://profile/{" or "user://profile/{}" contain braces but aren't
            // recognized as valid templates. Reject them rather than silently registering as static resources.
            throw new ArgumentException("Resource URI contains malformed template syntax (unmatched or empty braces).", nameof(uri));
        }

        // Replace template expressions with a safe placeholder to validate overall URI shape (scheme, etc.).
        var validationUri = isTemplate
            ? TemplateExpressionMatcher().Replace(uri, "template")
            : uri;

        if (!Uri.TryCreate(validationUri, UriKind.Absolute, out var parsedUri) || !parsedUri.IsAbsoluteUri)
        {
            throw new ArgumentException($"Invalid resource URI format: '{uri}'", nameof(uri));
        }
    }

    private static void ValidateTemplateSyntax(string uriTemplate)
    {
        // Reject directly adjacent template expressions (e.g. {a}{b}) where there is
        // no text at all between them – the regex cannot split values without some
        // literal separator. Literal text like "items" in {category}items{tag} is
        // allowed; the regex engine will use it as a fixed anchor via backtracking.
        var matches = TemplateExpressionMatcher().Matches(uriTemplate);
        for (int i = 0; i < matches.Count - 1; i++)
        {
            var endOfCurrent = matches[i].Index + matches[i].Length;
            var startOfNext = matches[i + 1].Index;

            if (endOfCurrent == startOfNext)
            {
                throw new ArgumentException(
                    "Resource URI template expressions must be separated by at least one literal character or delimiter.",
                    nameof(uriTemplate));
            }
        }
    }

    private static void ValidateParameters(IReadOnlyList<string> parameters, string uriTemplate)
    {
        if (parameters.Count == 0)
        {
            throw new ArgumentException("Resource URI template must contain at least one parameter expression.", nameof(uriTemplate));
        }

        var sanitized = parameters.Select(SanitizeParameterName).ToList();
        var collisions = parameters.Zip(sanitized, (orig, san) => (Original: orig, Sanitized: san))
            .GroupBy(x => x.Sanitized, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (collisions.Count > 0)
        {
            var first = collisions[0];
            var names = string.Join("', '", first.Select(x => x.Original));
            throw new ArgumentException(
                $"Resource URI template has duplicate or colliding parameter names: '{names}' all resolve to '{first.Key}'.",
                nameof(uriTemplate));
        }

        for (var idx = 0; idx < parameters.Count; idx++)
        {
            var param = parameters[idx];
            if (string.IsNullOrWhiteSpace(param))
            {
                throw new ArgumentException("Resource URI template parameters cannot be empty.", nameof(uriTemplate));
            }

            if (param.IndexOfAny(['/', '#', '?', '&', ' ']) >= 0)
            {
                throw new ArgumentException($"Resource URI template parameter '{param}' contains invalid characters.", nameof(uriTemplate));
            }

            if (param.Contains("%7B", StringComparison.OrdinalIgnoreCase) || param.Contains("%7D", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Resource URI template parameter '{param}' contains encoded braces.", nameof(uriTemplate));
            }
        }
    }

    private static Regex BuildRegexCore(string uriTemplate)
    {
        var sb = new System.Text.StringBuilder();
        int i = 0;
        while (i < uriTemplate.Length)
        {
            var ch = uriTemplate[i];
            if (ch == '{')
            {
                var end = uriTemplate.IndexOf('}', i + 1);
                if (end < 0)
                {
                    throw new ArgumentException("Resource URI template has unbalanced braces.", nameof(uriTemplate));
                }

                var parameter = uriTemplate[(i + 1)..end];
                var safeName = SanitizeParameterName(parameter);

                // Stop set depends on location: in query string, stop at & or #; otherwise at / ? #
                var previous = i == 0 ? '\0' : uriTemplate[i - 1];
                var stopSet = previous is '?' or '&' ? "[^&#]+" : "[^/?#]+";

                sb.Append($"(?<{safeName}>{stopSet})");
                i = end + 1;
                continue;
            }

            sb.Append(Regex.Escape(ch.ToString()));
            i++;
        }

        var pattern = "^" + sb + "$";
        return new Regex(
            pattern,
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
    }
}
