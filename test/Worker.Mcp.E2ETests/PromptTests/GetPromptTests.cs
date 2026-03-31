// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.PromptTests;

public class GetPromptTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task GetPrompt_CodeReview_ReturnsPromptResult(HttpTransportMode mode)
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
        Assert.Single(result.Messages);

        var message = result.Messages[0];
        Assert.Equal(Role.User, message.Role);
        var textContent = Assert.IsType<TextContentBlock>(message.Content);
        Assert.Contains("def hello(): print('world')", textContent.Text);
        Assert.Contains("python", textContent.Text);

        TestOutputHelper.WriteLine($"Message: {textContent.Text}");
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task GetPrompt_CodeReview_WithoutRequiredArguments_ThrowsError(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);

        await Assert.ThrowsAsync<McpProtocolException>(async () =>
            await client.GetPromptAsync(
                "code_review",
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(HttpTransportMode.Sse)]
    [InlineData(HttpTransportMode.AutoDetect)]
    [InlineData(HttpTransportMode.StreamableHttp)]
    public async Task GetPrompt_Summarize_ReturnsWrappedStringAsUserMessage(HttpTransportMode mode)
    {
        var client = await Fixture.CreateClientAsync(mode);

        var result = await client.GetPromptAsync(
            "summarize",
            new Dictionary<string, object?>
            {
                ["text"] = "The quick brown fox jumps over the lazy dog."
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Single(result.Messages);

        var message = result.Messages[0];
        Assert.Equal(Role.User, message.Role);
        var textContent = Assert.IsType<TextContentBlock>(message.Content);
        Assert.Contains("The quick brown fox jumps over the lazy dog.", textContent.Text);
        Assert.Contains("summary", textContent.Text);

        TestOutputHelper.WriteLine($"Summarize result: {textContent.Text}");
    }
}
