// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.PromptTests;

/// <summary>
/// End-to-end happy- and error-path coverage for prompt invocation through
/// the MCP protocol. Argument binding, default handling, and content
/// rendering for individual prompt shapes are covered by unit tests:
/// PromptInvocationContextConverterTests, PromptArgumentConverterTests,
/// PromptReturnValueBinderTests, DefaultPromptRegistryTests,
/// McpPromptListenerTests.
/// </summary>
public class GetPromptTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    [Theory]
    [MemberData(nameof(TransportModes.All), MemberType = typeof(TransportModes))]
    public async Task GetPrompt_HappyPath_ReturnsRenderedPrompt(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);

        var result = await client.GetPromptAsync(
            "code_review",
            new Dictionary<string, object?>
            {
                ["code"] = "def hello(): print('world')",
                ["language"] = "python"
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var message = Assert.Single(result.Messages);
        Assert.Equal(Role.User, message.Role);
        var content = Assert.IsType<TextContentBlock>(message.Content);
        Assert.Contains("def hello(): print('world')", content.Text);
        Assert.Contains("python", content.Text);
    }

    [Fact]
    public async Task GetPrompt_MissingRequiredArgument_Throws()
    {
        var client = await Fixture.CreateClientAsync(HttpTransportMode.StreamableHttp);

        await Assert.ThrowsAsync<McpProtocolException>(async () =>
            await client.GetPromptAsync(
                "code_review",
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetPrompt_NonExistent_Throws()
    {
        var client = await Fixture.CreateClientAsync(HttpTransportMode.StreamableHttp);

        await Assert.ThrowsAsync<McpProtocolException>(async () =>
            await client.GetPromptAsync(
                "non_existent_prompt",
                cancellationToken: TestContext.Current.CancellationToken));
    }
}
