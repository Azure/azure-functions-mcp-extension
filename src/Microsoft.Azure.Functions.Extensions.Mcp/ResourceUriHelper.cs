// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Helper methods for validating and inspecting MCP resource URIs, including templated URIs.
/// </summary>
internal static class ResourceUriHelper
{
    // RFC 6570 level 1 style placeholder: {param}. We intentionally keep the pattern simple
    // to allow single-level expressions while rejecting nested or empty braces.
    private static readonly Regex TemplateExpressionPattern = new(
        "\\{[^{}]+\\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Cache compiled templates to avoid repeated regex construction per request.
    private static readonly ConcurrentDictionary<string, TemplateInfo> TemplateCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Determines whether the URI contains RFC6570-style template expressions.
    /// </summary>
    public static bool IsTemplate(string uri) => TemplateExpressionPattern.IsMatch(uri);

    /// <summary>
    /// Exposes the compiled template expression regex for reuse (e.g., when building matchers).
    /// </summary>
    public static Regex TemplateExpressionRegex() => TemplateExpressionPattern;

    /// <summary>
    /// Returns the ordered list of parameter names declared in a URI template.
    /// </summary>
    public static IReadOnlyList<string> GetTemplateParameterNames(string uriTemplate) =>
        ParseTemplate(uriTemplate).Parameters;

    /// <summary>
    /// Builds a regex that matches the supplied URI template and exposes named capture groups for each parameter.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the template has duplicate or colliding parameter names.</exception>
    public static Regex BuildTemplateRegex(string uriTemplate)
    {
        var info = ParseTemplate(uriTemplate);
        if (!info.IsTemplate || info.Regex is null)
        {
            throw new ArgumentException("Resource URI must contain at least one template expression.", nameof(uriTemplate));
        }

        return info.Regex;
    }

    /// <summary>
    /// Sanitizes a parameter name for use in a regex named capture group.
    /// </summary>
    private static string SanitizeParameterName(string parameter) =>
        Regex.Replace(parameter, "[^A-Za-z0-9_]", "_", RegexOptions.CultureInvariant);

    /// <summary>
    /// Attempts to extract template parameter values from an actual URI using the provided template.
    /// </summary>
    public static bool TryExtractParameters(string uriTemplate, string actualUri, [NotNullWhen(true)] out IReadOnlyDictionary<string, string>? values)
    {
        values = null;
        var templateInfo = ParseTemplate(uriTemplate);
        if (!templateInfo.IsTemplate || templateInfo.Regex is null)
        {
            return false;
        }

        var match = templateInfo.Regex.Match(actualUri);
        if (!match.Success)
        {
            return false;
        }

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var groupName in templateInfo.Regex.GetGroupNames())
        {
            if (int.TryParse(groupName, out _))
            {
                continue; // skip numeric groups
            }

            dict[groupName] = match.Groups[groupName].Value;
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

        var template = ParseTemplate(uri);
        var isTemplate = template.IsTemplate;

        // Basic brace balance check for templates.
        if (isTemplate && uri.Count(c => c == '{') != uri.Count(c => c == '}'))
        {
            throw new ArgumentException("Resource URI template has unbalanced braces.", nameof(uri));
        }

        // Replace template expressions with a safe placeholder to validate overall URI shape (scheme, etc.).
        var validationUri = template.ValidationUri ?? uri;

        if (!Uri.TryCreate(validationUri, UriKind.Absolute, out var parsedUri) || !parsedUri.IsAbsoluteUri)
        {
            throw new ArgumentException($"Invalid resource URI format: '{uri}'", nameof(uri));
        }

        // ParseTemplate already enforces parameter rules.
    }

    private static TemplateInfo ParseTemplate(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (!IsTemplate(uri))
        {
            return new TemplateInfo(false, Array.Empty<string>(), null, null);
        }

        return TemplateCache.GetOrAdd(uri, static key =>
        {
            ValidateTemplateSyntax(key);
            var parameters = TemplateExpressionPattern.Matches(key)
                .Select(m => m.Value[1..^1])
                .ToArray();

            ValidateParameters(parameters, key);

            var regex = BuildRegexCore(key);
            var validationUri = TemplateExpressionPattern.Replace(key, "template");

            return new TemplateInfo(true, parameters, regex, validationUri);
        });
    }

    private static void ValidateTemplateSyntax(string uriTemplate)
    {
        // Prevent adjacent expressions without delimiters: ...}{...
        if (uriTemplate.Contains("}{", StringComparison.Ordinal))
        {
            throw new ArgumentException("Resource URI template expressions must be separated by a delimiter.", nameof(uriTemplate));
        }
    }

    private static void ValidateParameters(string[] parameters, string uriTemplate)
    {
        if (parameters.Length == 0)
        {
            throw new ArgumentException("Resource URI template must contain at least one parameter expression.", nameof(uriTemplate));
        }

        var sanitized = parameters.Select(SanitizeParameterName).ToList();
        if (sanitized.Distinct(StringComparer.OrdinalIgnoreCase).Count() != sanitized.Count)
        {
            throw new ArgumentException("Resource URI template has duplicate or colliding parameter names.", nameof(uriTemplate));
        }

        for (var idx = 0; idx < parameters.Length; idx++)
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

    private sealed record TemplateInfo(
        bool IsTemplate,
        IReadOnlyList<string> Parameters,
        Regex? Regex,
        string? ValidationUri);
}
