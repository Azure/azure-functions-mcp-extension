// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.PromptTests;

/// <summary>
/// Smoke-tests that ListPrompts flows end-to-end. Prompt metadata content
/// (titles, descriptions, argument shape) is covered by unit tests against
/// the prompt builder and registry: McpPromptBuilderTests,
/// DefaultPromptRegistryTests, AppMetadataSerializationTests.
/// </summary>
public class ListPromptTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    [Theory]
    [MemberData(nameof(TransportModes.All), MemberType = typeof(TransportModes))]
    public async Task ListPrompts_ReturnsExpectedPrompts(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);
        var prompts = await client.ListPromptsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(prompts);
        Assert.True(prompts.Count >= 6, $"Expected at least 6 prompts but found {prompts.Count}");

        Assert.Contains(prompts, p => p.Name == "code_review");
        Assert.Contains(prompts, p => p.Name == "summarize");
        Assert.Contains(prompts, p => p.Name == "fluent_prompt");
    }
}
