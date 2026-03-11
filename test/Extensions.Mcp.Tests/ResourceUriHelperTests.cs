// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class ResourceUriHelperTests
{
    #region IsTemplate Tests

    [Theory]
    [InlineData("test://resource/{id}", true)]
    [InlineData("test://users/{userId}/posts/{postId}", true)]
    [InlineData("file://logs/{date}", true)]
    [InlineData("test://resource/1", false)]
    [InlineData("test://users/123/posts/456", false)]
    public void IsTemplate_ReturnsExpectedResult(string uri, bool expected)
    {
        var result = ResourceUriHelper.IsTemplate(uri);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsTemplate_WithEmptyBraces_ReturnsFalse()
    {
        // Empty braces {} don't match the pattern requiring at least one character
        var result = ResourceUriHelper.IsTemplate("test://resource/{}");

        Assert.False(result);
    }

    #endregion

    #region GetTemplateParameterNames Tests

    [Fact]
    public void GetTemplateParameterNames_WithSingleParameter_ReturnsParameterName()
    {
        var names = ResourceUriHelper.GetTemplateParameterNames("test://resource/{id}");

        Assert.Single(names);
        Assert.Equal("id", names[0]);
    }

    [Fact]
    public void GetTemplateParameterNames_WithMultipleParameters_ReturnsAllNames()
    {
        var names = ResourceUriHelper.GetTemplateParameterNames("test://users/{userId}/posts/{postId}");

        Assert.Equal(2, names.Count);
        Assert.Equal("userId", names[0]);
        Assert.Equal("postId", names[1]);
    }

    [Fact]
    public void GetTemplateParameterNames_WithNoParameters_ReturnsEmptyList()
    {
        var names = ResourceUriHelper.GetTemplateParameterNames("test://resource/123");

        Assert.Empty(names);
    }

    [Fact]
    public void GetTemplateParameterNames_WithNullUri_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ResourceUriHelper.GetTemplateParameterNames(null!));
    }

    [Fact]
    public void GetTemplateParameterNames_WithSpecialCharacters_ReturnsOriginalNames()
    {
        var names = ResourceUriHelper.GetTemplateParameterNames("test://items/{user-id}/posts/{post.id}");

        Assert.Equal(2, names.Count);
        Assert.Equal("user-id", names[0]);
        Assert.Equal("post.id", names[1]);
    }

    #endregion

    #region BuildTemplateRegex Tests

    [Fact]
    public void BuildTemplateRegex_WithSingleParameter_BuildsMatchingRegex()
    {
        var regex = ResourceUriHelper.BuildTemplateRegex("test://resource/{id}");

        Assert.Matches(regex, "test://resource/123");
        Assert.Matches(regex, "test://resource/abc");
        Assert.DoesNotMatch(regex, "test://other/123");
    }

    [Fact]
    public void BuildTemplateRegex_WithMultipleParameters_BuildsMatchingRegex()
    {
        var regex = ResourceUriHelper.BuildTemplateRegex("test://users/{userId}/posts/{postId}");

        Assert.Matches(regex, "test://users/123/posts/456");
        Assert.Matches(regex, "test://users/john/posts/my-post");
        Assert.DoesNotMatch(regex, "test://users/123");
    }

    [Fact]
    public void BuildTemplateRegex_IsCaseInsensitive()
    {
        var regex = ResourceUriHelper.BuildTemplateRegex("test://Resource/{id}");

        Assert.Matches(regex, "TEST://RESOURCE/123");
        Assert.Matches(regex, "test://resource/123");
    }

    [Fact]
    public void BuildTemplateRegex_CapturesParameterValues()
    {
        var regex = ResourceUriHelper.BuildTemplateRegex("test://users/{userId}/posts/{postId}");
        var match = regex.Match("test://users/john/posts/hello-world");

        Assert.True(match.Success);
        Assert.Equal("john", match.Groups["userId"].Value);
        Assert.Equal("hello-world", match.Groups["postId"].Value);
    }

    [Fact]
    public void BuildTemplateRegex_WithUnbalancedBraces_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ResourceUriHelper.BuildTemplateRegex("test://resource/{id"));
    }

    [Fact]
    public void BuildTemplateRegex_WithNullUri_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ResourceUriHelper.BuildTemplateRegex(null!));
    }

    [Fact]
    public void BuildTemplateRegex_WithSpecialCharactersInUri_EscapesThem()
    {
        var regex = ResourceUriHelper.BuildTemplateRegex("test://resource.name/{id}?query=true");

        Assert.Matches(regex, "test://resource.name/123?query=true");
        Assert.DoesNotMatch(regex, "test://resourceXname/123?query=true");
    }

    [Fact]
    public void BuildTemplateRegex_WithDuplicateParameters_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ResourceUriHelper.BuildTemplateRegex("test://items/{id}/subitems/{id}"));
        Assert.Contains("duplicate or colliding", exception.Message);
    }

    [Fact]
    public void BuildTemplateRegex_WithCollidingParameterNames_ThrowsArgumentException()
    {
        // user-id and user.id both sanitize to user_id
        var exception = Assert.Throws<ArgumentException>(
            () => ResourceUriHelper.BuildTemplateRegex("test://items/{user-id}/posts/{user.id}"));
        Assert.Contains("duplicate or colliding", exception.Message);
        Assert.Contains("user-id", exception.Message);
        Assert.Contains("user.id", exception.Message);
    }

    [Fact]
    public void BuildTemplateRegex_WithPathSeparatedParameters_MatchesCorrectly()
    {
        var regex = ResourceUriHelper.BuildTemplateRegex("test://items/{category}/{id}");

        Assert.Matches(regex, "test://items/books/123");
        var match = regex.Match("test://items/books/123");
        Assert.Equal("books", match.Groups["category"].Value);
        Assert.Equal("123", match.Groups["id"].Value);
    }

    [Fact]
    public void BuildTemplateRegex_WithQueryStringParameter_MatchesCorrectly()
    {
        var regex = ResourceUriHelper.BuildTemplateRegex("test://search?q={query}");

        Assert.Matches(regex, "test://search?q=hello");
        var match = regex.Match("test://search?q=hello");
        Assert.Equal("hello", match.Groups["query"].Value);
    }

    [Fact]
    public void BuildTemplateRegex_QueryParameter_StopsAtAmpersand()
    {
        // Template with multiple query params - verifies first param stops at &
        var regex = ResourceUriHelper.BuildTemplateRegex("test://search?q={query}&lang={lang}");

        Assert.Matches(regex, "test://search?q=hello&lang=en");
        var match = regex.Match("test://search?q=hello&lang=en");
        Assert.Equal("hello", match.Groups["query"].Value);
        Assert.Equal("en", match.Groups["lang"].Value);
    }

    [Fact]
    public void BuildTemplateRegex_ParameterDoesNotMatchPathSeparator()
    {
        // With segment-aware pattern, {id} should NOT match across path separators
        var regex = ResourceUriHelper.BuildTemplateRegex("test://items/{id}");

        Assert.Matches(regex, "test://items/123");
        Assert.DoesNotMatch(regex, "test://items/123/extra");
    }

    [Fact]
    public void BuildTemplateRegex_WithAdjacentParameters_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => ResourceUriHelper.BuildTemplateRegex("test://items/{a}{b}"));
        Assert.Contains("must be separated", exception.Message);
    }

    [Fact]
    public void BuildTemplateRegex_WithLiteralTextBetweenExpressions_Succeeds()
    {
        var regex = ResourceUriHelper.BuildTemplateRegex("test://items/{category}items{tag}");

        var match = regex.Match("test://items/booksitemsfiction");
        Assert.True(match.Success);
        Assert.Equal("books", match.Groups["category"].Value);
        Assert.Equal("fiction", match.Groups["tag"].Value);
    }

    #endregion

    #region TryExtractParameters Tests

    [Fact]
    public void TryExtractParameters_WithMatchingUri_ReturnsTrue()
    {
        var result = ResourceUriHelper.TryExtractParameters(
            "test://users/{userId}",
            "test://users/123",
            out var values);

        Assert.True(result);
        Assert.NotNull(values);
        Assert.Single(values);
        Assert.Equal("123", values["userId"]);
    }

    [Fact]
    public void TryExtractParameters_WithMultipleParameters_ExtractsAll()
    {
        var result = ResourceUriHelper.TryExtractParameters(
            "test://users/{userId}/posts/{postId}",
            "test://users/john/posts/my-first-post",
            out var values);

        Assert.True(result);
        Assert.NotNull(values);
        Assert.Equal(2, values.Count);
        Assert.Equal("john", values["userId"]);
        Assert.Equal("my-first-post", values["postId"]);
    }

    [Fact]
    public void TryExtractParameters_WithNonMatchingUri_ReturnsFalse()
    {
        var result = ResourceUriHelper.TryExtractParameters(
            "test://users/{userId}",
            "test://posts/123",
            out var values);

        Assert.False(result);
        Assert.Null(values);
    }

    [Fact]
    public void TryExtractParameters_WithNonTemplateUri_ReturnsFalse()
    {
        var result = ResourceUriHelper.TryExtractParameters(
            "test://users/123",
            "test://users/123",
            out var values);

        Assert.False(result);
        Assert.Null(values);
    }

    [Fact]
    public void TryExtractParameters_IsCaseInsensitive()
    {
        var result = ResourceUriHelper.TryExtractParameters(
            "test://Users/{userId}",
            "TEST://USERS/John",
            out var values);

        Assert.True(result);
        Assert.NotNull(values);
        Assert.Equal("John", values["userId"]);
    }

    [Fact]
    public void TryExtractParameters_WithSpecialCharactersInValue_ExtractsCorrectly()
    {
        var result = ResourceUriHelper.TryExtractParameters(
            "test://files/{filename}",
            "test://files/my-file_v2.0.txt",
            out var values);

        Assert.True(result);
        Assert.NotNull(values);
        Assert.Equal("my-file_v2.0.txt", values["filename"]);
    }

    [Fact]
    public void TryExtractParameters_WithEncodedCharacters_ExtractsAsIs()
    {
        var result = ResourceUriHelper.TryExtractParameters(
            "test://search/{query}",
            "test://search/hello%20world",
            out var values);

        Assert.True(result);
        Assert.NotNull(values);
        Assert.Equal("hello%20world", values["query"]);
    }

    [Fact]
    public void TryExtractParameters_WithThreeParameters_ExtractsAll()
    {
        var result = ResourceUriHelper.TryExtractParameters(
            "test://org/{org}/repo/{repo}/branch/{branch}",
            "test://org/microsoft/repo/vscode/branch/main",
            out var values);

        Assert.True(result);
        Assert.NotNull(values);
        Assert.Equal(3, values.Count);
        Assert.Equal("microsoft", values["org"]);
        Assert.Equal("vscode", values["repo"]);
        Assert.Equal("main", values["branch"]);
    }

    [Fact]
    public void TryExtractParameters_QueryStopsAtAmpersand()
    {
        // Template with multiple query params - verifies first param stops at &
        var result = ResourceUriHelper.TryExtractParameters(
            "test://search?q={query}&lang={lang}",
            "test://search?q=hello&lang=en",
            out var values);

        Assert.True(result);
        Assert.Equal("hello", values!["query"]);
        Assert.Equal("en", values["lang"]);
    }

    [Fact]
    public void TryExtractParameters_WithHyphenatedParameter_ReturnsOriginalName()
    {
        var result = ResourceUriHelper.TryExtractParameters(
            "test://items/{user-id}",
            "test://items/123",
            out var values);

        Assert.True(result);
        Assert.NotNull(values);
        Assert.True(values.ContainsKey("user-id"));
        Assert.Equal("123", values["user-id"]);
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_WithValidAbsoluteUri_DoesNotThrow()
    {
        ResourceUriHelper.Validate("test://resource/1");
        ResourceUriHelper.Validate("https://example.com/api");
        ResourceUriHelper.Validate("file://logs/2024-01-01");
    }

    [Fact]
    public void Validate_WithValidTemplateUri_DoesNotThrow()
    {
        ResourceUriHelper.Validate("test://resource/{id}");
        ResourceUriHelper.Validate("test://users/{userId}/posts/{postId}");
    }

    [Fact]
    public void Validate_WithNullUri_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => ResourceUriHelper.Validate(null!));
        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void Validate_WithEmptyUri_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => ResourceUriHelper.Validate(""));
        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void Validate_WithWhitespaceUri_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => ResourceUriHelper.Validate("   "));
        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void Validate_WithUnbalancedBraces_ThrowsArgumentException()
    {
        // Must have at least one valid template expression for the brace check to run
        var exception = Assert.Throws<ArgumentException>(() => ResourceUriHelper.Validate("test://resource/{id}/{"));
        Assert.Contains("unbalanced braces", exception.Message);
    }

    [Fact]
    public void Validate_WithInvalidUriFormat_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => ResourceUriHelper.Validate("not a valid uri"));
        Assert.Contains("Invalid resource URI format", exception.Message);
    }

    [Fact]
    public void Validate_WithEmptyTemplateParameter_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => ResourceUriHelper.Validate("test://resource/{ }"));
        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public void Validate_WithDuplicateParameters_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ResourceUriHelper.Validate("test://items/{id}/subitems/{id}"));
        Assert.Contains("duplicate or colliding", exception.Message);
    }

    [Fact]
    public void Validate_WithCollidingParameterNames_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ResourceUriHelper.Validate("test://items/{user-id}/posts/{user.id}"));
        Assert.Contains("duplicate or colliding", exception.Message);
        Assert.Contains("user-id", exception.Message);
        Assert.Contains("user.id", exception.Message);
    }

    [Fact]
    public void Validate_WithValidMultipleDistinctParameters_DoesNotThrow()
    {
        ResourceUriHelper.Validate("test://items/{category}/{id}/{name}");
    }

    [Fact]
    public void Validate_WithAdjacentParameters_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => ResourceUriHelper.Validate("test://items/{a}{b}"));
        Assert.Contains("separated", exception.Message);
    }

    [Fact]
    public void Validate_WithInvalidCharactersInParameter_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => ResourceUriHelper.Validate("test://items/{bad/name}"));
        Assert.Contains("invalid characters", exception.Message);
    }

    [Fact]
    public void Validate_WithEncodedBracesInParameter_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => ResourceUriHelper.Validate("test://items/{%7Bbad%7D}"));
        Assert.Contains("encoded braces", exception.Message);
    }

    [Fact]
    public void Validate_WithLiteralTextBetweenExpressions_DoesNotThrow()
    {
        ResourceUriHelper.Validate("test://items/{category}items{tag}");
    }

    [Theory]
    [InlineData("user://profile/{")]
    [InlineData("user://profile/}")]
    [InlineData("user://profile/{}")]
    [InlineData("user://profile/{}/test")]
    public void Validate_WithMalformedBraces_ThrowsArgumentException(string uri)
    {
        var exception = Assert.Throws<ArgumentException>(() => ResourceUriHelper.Validate(uri));
        Assert.Contains("malformed template syntax", exception.Message);
    }

    #endregion
}
