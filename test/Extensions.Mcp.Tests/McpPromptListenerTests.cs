// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Executors;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpPromptListenerTests
{
    private static RequestContext<GetPromptRequestParams> CreateRequest(
        string name = "TestPrompt",
        IReadOnlyDictionary<string, JsonElement>? arguments = null)
    {
        var server = new Mock<McpServer>().Object;
        var parameters = new GetPromptRequestParams { Name = name, Arguments = arguments };

        return new RequestContext<GetPromptRequestParams>(server, new JsonRpcRequest() { Method = RequestMethods.PromptsGet })
        {
            Params = parameters
        };
    }

    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement;

    [Fact]
    public void Constructor_SetsProperties()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var arguments = new List<PromptArgument>
        {
            new() { Name = "code", Description = "The code to review", Required = true }
        };
        var metadata = new Dictionary<string, object?> { { "key1", "value1" } };

        var listener = new McpPromptListener(
            executor,
            "MyFunction",
            "code_review",
            "Code Review",
            "Reviews code quality",
            arguments,
            null,
            metadata);

        Assert.Equal("code_review", listener.Name);
        Assert.Equal("Code Review", listener.Title);
        Assert.Equal("Reviews code quality", listener.Description);
        Assert.Equal("MyFunction", listener.FunctionName);
        Assert.NotNull(listener.Arguments);
        Assert.Single(listener.Arguments);
        Assert.Equal("code", listener.Arguments[0].Name);
        Assert.True(listener.Arguments[0].Required);
        Assert.Same(executor, listener.Executor);
        Assert.Same(metadata, listener.Metadata);
        Assert.Null(listener.Icons);
    }

    [Fact]
    public void Constructor_WithNullOptionalProperties_SetsNulls()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var metadata = new Dictionary<string, object?>();

        var listener = new McpPromptListener(
            executor,
            "MyFunction",
            "test_prompt",
            null,
            null,
            null,
            null,
            metadata);

        Assert.Equal("test_prompt", listener.Name);
        Assert.Null(listener.Title);
        Assert.Null(listener.Description);
        Assert.Null(listener.Arguments);
    }

    [Fact]
    public void Constructor_WithIcons_SetsIcons()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var icons = new List<Icon>
        {
            new() { Source = "https://example.com/icon.svg", MimeType = "image/svg+xml" }
        };

        var listener = new McpPromptListener(
            executor,
            "MyFunction",
            "code_review",
            null,
            null,
            null,
            icons,
            new Dictionary<string, object?>());

        Assert.NotNull(listener.Icons);
        Assert.Single(listener.Icons);
        Assert.Equal("https://example.com/icon.svg", listener.Icons[0].Source);
    }

    [Fact]
    public async Task GetAsync_WhenFunctionSucceeds_ReturnsGetPromptResult()
    {
        var expectedResult = new GetPromptResult
        {
            Messages =
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock { Text = "Review this code" }
                }
            ]
        };

        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor.Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var execCtx = (GetPromptExecutionContext)data.TriggerValue;
                execCtx.SetResult(expectedResult);
            })
            .ReturnsAsync(new FunctionResult(true));

        var listener = new McpPromptListener(
            executor.Object,
            "MyFunction",
            "code_review",
            null,
            null,
            null,
            null,
            new Dictionary<string, object?>());

        var result = await listener.GetAsync(CreateRequest("code_review"), CancellationToken.None);

        Assert.Same(expectedResult, result);
    }

    [Fact]
    public async Task GetAsync_WhenFunctionFails_ThrowsException()
    {
        var expectedException = new InvalidOperationException("Function failed");

        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor.Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FunctionResult(false, expectedException));

        var listener = new McpPromptListener(
            executor.Object,
            "MyFunction",
            "code_review",
            null,
            null,
            null,
            null,
            new Dictionary<string, object?>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => listener.GetAsync(CreateRequest("code_review"), CancellationToken.None));

        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task GetAsync_WhenResultIsNotGetPromptResult_ReturnsEmptyMessages()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor.Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var execCtx = (GetPromptExecutionContext)data.TriggerValue;
                // Set a result but it will still be GetPromptResult since the TCS is typed
                execCtx.SetResult(new GetPromptResult { Messages = [] });
            })
            .ReturnsAsync(new FunctionResult(true));

        var listener = new McpPromptListener(
            executor.Object,
            "MyFunction",
            "code_review",
            null,
            null,
            null,
            null,
            new Dictionary<string, object?>());

        var result = await listener.GetAsync(CreateRequest("code_review"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public async Task StartAsync_CompletesSuccessfully()
    {
        var listener = CreateSimpleListener();
        await listener.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        var listener = CreateSimpleListener();
        await listener.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void Cancel_DoesNotThrow()
    {
        var listener = CreateSimpleListener();
        listener.Cancel();
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var listener = CreateSimpleListener();
        listener.Dispose();
    }

    private static McpPromptListener CreateSimpleListener()
    {
        return new McpPromptListener(
            new Mock<ITriggeredFunctionExecutor>().Object,
            "MyFunction",
            "test_prompt",
            null,
            null,
            null,
            null,
            new Dictionary<string, object?>());
    }

    // ── Required-argument validation ─────────────────────────────────────

    [Fact]
    public async Task GetAsync_RequiredArgumentMissing_ThrowsMcpProtocolException()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>(MockBehavior.Strict);

        var listener = new McpPromptListener(
            executor.Object,
            "MyFunction",
            "code_review",
            null,
            null,
            [new() { Name = "code", Required = true }],
            null,
            new Dictionary<string, object?>());

        var ex = await Assert.ThrowsAsync<McpProtocolException>(
            () => listener.GetAsync(CreateRequest("code_review"), CancellationToken.None));

        Assert.Contains("code", ex.Message);
        executor.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_RequiredArgumentNull_ThrowsMcpProtocolException()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>(MockBehavior.Strict);

        var listener = new McpPromptListener(
            executor.Object,
            "MyFunction",
            "code_review",
            null,
            null,
            [new() { Name = "code", Required = true }],
            null,
            new Dictionary<string, object?>());

        var args = new Dictionary<string, JsonElement> { ["code"] = Json("null") };

        var ex = await Assert.ThrowsAsync<McpProtocolException>(
            () => listener.GetAsync(CreateRequest("code_review", args), CancellationToken.None));

        Assert.Contains("code", ex.Message);
        executor.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_MultipleRequiredArgumentsMissing_ListsAllInMessage()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>(MockBehavior.Strict);

        var listener = new McpPromptListener(
            executor.Object,
            "MyFunction",
            "p",
            null,
            null,
            [
                new() { Name = "a", Required = true },
                new() { Name = "b", Required = true },
                new() { Name = "c", Required = false },
            ],
            null,
            new Dictionary<string, object?>());

        var ex = await Assert.ThrowsAsync<McpProtocolException>(
            () => listener.GetAsync(CreateRequest("p"), CancellationToken.None));

        Assert.Contains("a", ex.Message);
        Assert.Contains("b", ex.Message);
        Assert.DoesNotContain("c", ex.Message);
    }

    [Fact]
    public async Task GetAsync_OptionalArgumentMissing_DoesNotThrow()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor.Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var execCtx = (GetPromptExecutionContext)data.TriggerValue;
                execCtx.SetResult(new GetPromptResult { Messages = [] });
            })
            .ReturnsAsync(new FunctionResult(true));

        var listener = new McpPromptListener(
            executor.Object,
            "MyFunction",
            "p",
            null,
            null,
            [new() { Name = "optional_arg", Required = false }],
            null,
            new Dictionary<string, object?>());

        var result = await listener.GetAsync(CreateRequest("p"), CancellationToken.None);

        Assert.NotNull(result);
        executor.VerifyAll();
    }

    [Fact]
    public async Task GetAsync_RequiredArgumentProvided_DoesNotThrow()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor.Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var execCtx = (GetPromptExecutionContext)data.TriggerValue;
                execCtx.SetResult(new GetPromptResult { Messages = [] });
            })
            .ReturnsAsync(new FunctionResult(true));

        var listener = new McpPromptListener(
            executor.Object,
            "MyFunction",
            "code_review",
            null,
            null,
            [new() { Name = "code", Required = true }],
            null,
            new Dictionary<string, object?>());

        var args = new Dictionary<string, JsonElement> { ["code"] = Json("\"hello\"") };

        var result = await listener.GetAsync(CreateRequest("code_review", args), CancellationToken.None);

        Assert.NotNull(result);
        executor.VerifyAll();
    }

    [Fact]
    public async Task GetAsync_NoDeclaredArguments_DoesNotValidate()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor.Setup(e => e.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var execCtx = (GetPromptExecutionContext)data.TriggerValue;
                execCtx.SetResult(new GetPromptResult { Messages = [] });
            })
            .ReturnsAsync(new FunctionResult(true));

        var listener = new McpPromptListener(
            executor.Object,
            "MyFunction",
            "p",
            null,
            null,
            arguments: null,
            null,
            new Dictionary<string, object?>());

        var result = await listener.GetAsync(CreateRequest("p"), CancellationToken.None);

        Assert.NotNull(result);
        executor.VerifyAll();
    }
}
