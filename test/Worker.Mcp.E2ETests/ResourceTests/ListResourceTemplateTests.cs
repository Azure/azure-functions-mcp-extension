// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ResourceTests;

public class ListResourceTemplateTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListResourceTemplates_ReturnsTemplates(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var templates = await client.ListResourceTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(templates);
        Assert.True(templates.Count > 0, "Expected at least one resource template to be returned");
        TestOutputHelper.WriteLine($"Found {templates.Count} resource templates");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListResourceTemplates_ReturnsUserProfileTemplate(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var templates = await client.ListResourceTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(templates);

        // Should contain the UserProfileResourceTemplate
        var userProfileTemplate = templates.FirstOrDefault(t => t.UriTemplate == "user://profile/{name}");
        Assert.NotNull(userProfileTemplate);
        Assert.Equal("userProfile", userProfileTemplate.Name);
        Assert.Equal("User profile resource", userProfileTemplate.Description);
        Assert.Equal("application/json", userProfileTemplate.MimeType);

        TestOutputHelper.WriteLine($"Templates: {string.Join(", ", templates.Select(t => t.UriTemplate))}");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task DefaultListResourceTemplates_NotIncludedInListResources(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);

        // Templates should NOT appear in regular resources list
        var resources = await client.ListResourcesAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.DoesNotContain(resources, r => r.Uri.Contains("{"));

        // Templates should appear in templates list
        var templates = await client.ListResourceTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(templates, t => t.UriTemplate.Contains("{"));

        TestOutputHelper.WriteLine($"Resources (no templates): {string.Join(", ", resources.Select(r => r.Uri))}");
        TestOutputHelper.WriteLine($"Templates: {string.Join(", ", templates.Select(t => t.UriTemplate))}");
    }
}
