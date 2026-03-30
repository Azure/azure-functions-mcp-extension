// Copyright(c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol.Protocol;
using System.Globalization;
using System.Text;
using ModelContextProtocol;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ToolTests;

/// <summary>
/// Tool invocation tests that make direct HTTP requests to the default server.
/// </summary>
public class CallToolTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    private Uri AppRootEndpoint => Fixture.AppRootEndpoint ?? throw new InvalidOperationException("AppRootEndpoint is not set.");

    // ── Simple tools ────────────────────────────────────────────────────────

    [Fact]
    public async Task EchoTool_ReturnsMessage()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "EchoTool", new { message = "hello-world" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("hello-world", response);
    }

    [Fact]
    public async Task EchoWithDefault_WithArgument_ReturnsArgument()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "EchoWithDefault", new { message = "custom-value" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("custom-value", response);
    }

    [Fact]
    public async Task EchoWithDefault_WithoutArgument_ReturnsDefault()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "EchoWithDefault", new { });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("(default-message)", response);
    }

    [Fact]
    public async Task VoidTool_CompletesSuccessfully()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "VoidTool", new { input = "test-input" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
    }

    // ── Typed parameter tools ───────────────────────────────────────────────

    [Fact]
    public async Task TypedParametersTool_ReturnsFormattedSummary()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "TypedParametersTool", new
        {
            name = "Alice",
            job = "FullTime",
            age = 30,
            isActive = true
        });

        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("Alice", response);
        Assert.Contains("FullTime", response);
    }

    [Fact]
    public async Task CollectionParametersTool_ReturnsContents()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "CollectionParametersTool", new
        {
            tags = new[] { "urgent", "review" },
            scores = new[] { 10, 20, 30 }
        });

        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("urgent", response);
        Assert.Contains("review", response);
    }

    [Fact]
    public async Task GuidAndDateTimeTool_ReturnsFormattedValues()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "GuidAndDateTimeTool", new
        {
            id = "9fe500ac-e415-4c59-a766-d6378ebd7acd",
            timestamp = "1995-10-13T14:45:32Z"
        });

        var expectedDate = DateTimeOffset.Parse("1995-10-13T14:45:32Z", null, DateTimeStyles.AssumeUniversal)
            .UtcDateTime
            .ToString("o", CultureInfo.InvariantCulture);

        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("9fe500ac-e415-4c59-a766-d6378ebd7acd", response);
        Assert.Contains(expectedDate, response);
    }

    // ── Content return tools ────────────────────────────────────────────────

    [Fact]
    public async Task TextContentTool_ReturnsPlainText()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "TextContentTool", new { });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("plain text content", response);
    }

    [Fact]
    public async Task ImageContentTool_ReturnsImageBlock()
    {
        var imageDataPath = GetTestDataFilePath("image-base64.txt");
        var data = await File.ReadAllTextAsync(imageDataPath, TestContext.Current.CancellationToken);

        var request = CallToolHelper.CreateToolCallRequest(1, "ImageContentTool", new { data, mimeType = "image/jpeg" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);

        var result = CallToolResultHelper.ParseSseToCallToolResult(response, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(result);
        Assert.Single(result.Content);
        Assert.Contains(result.Content, block => block is ImageContentBlock imageBlock
            && imageBlock.MimeType == "image/jpeg"
            && Encoding.UTF8.GetString(imageBlock.Data.Span).StartsWith(data.Substring(0, 20)));
    }

    [Fact]
    public async Task ResourceLinkTool_ReturnsResourceLink()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "ResourceLinkTool", new { });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);

        var result = CallToolResultHelper.ParseSseToCallToolResult(response, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(result);
        Assert.Single(result.Content);
        Assert.Contains(result.Content, block => block is ResourceLinkBlock linkBlock
            && linkBlock.Uri == "file://logo.png");
    }

    [Fact]
    public async Task MultiContentTool_ReturnsMultipleContentBlocks()
    {
        var imageDataPath = GetTestDataFilePath("image-base64.txt");
        var data = await File.ReadAllTextAsync(imageDataPath, TestContext.Current.CancellationToken);

        var request = CallToolHelper.CreateToolCallRequest(1, "MultiContentTool", new { data, mimeType = "image/jpeg" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);

        var result = CallToolResultHelper.ParseSseToCallToolResult(response, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal(3, result.Content.Count);

        Assert.Contains(result.Content, block => block is TextContentBlock textBlock && textBlock.Text == "Here is an image for you!");
        Assert.Contains(result.Content, block => block is ResourceLinkBlock linkBlock && linkBlock.Uri == "https://www.example.com/");
        Assert.Contains(result.Content, block => block is ImageContentBlock imageBlock && imageBlock.MimeType == "image/jpeg" && Encoding.UTF8.GetString(imageBlock.Data.Span).StartsWith(data.Substring(0, 20)));
    }

    [Fact]
    public async Task StructuredContentTool_ReturnsStructuredContent()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "StructuredContentTool", new { });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);

        var result = CallToolResultHelper.ParseSseToCallToolResult(response, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal(2, result.Content.Count);
        Assert.Contains(result.Content, block => block is TextContentBlock);
        Assert.Contains(result.Content, block => block is ImageContentBlock imageBlock && imageBlock.MimeType == "image/png");

        Assert.NotNull(result.StructuredContent);
        var structuredContent = result.StructuredContent.ToString();
        Assert.Contains("ImageId", structuredContent);
        Assert.Contains("logo", structuredContent);
        Assert.Contains("Format", structuredContent);
        Assert.Contains("png", structuredContent);
    }

    // ── POCO tools ──────────────────────────────────────────────────────────

    [Fact]
    public async Task PocoInputTool_AcceptsPoco_ReturnsGreeting()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "PocoInputTool", new
        {
            Name = "Alice",
            Age = 30,
            IsPremium = true
        });

        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("Alice", response);
        Assert.Contains("30", response);
        Assert.Contains("True", response);
    }

    [Fact]
    public async Task PocoOutputTool_ReturnsMcpContentStructuredOutput()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "PocoOutputTool", new { city = "Seattle" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);

        var result = CallToolResultHelper.ParseSseToCallToolResult(response, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(result);

        // [McpContent] produces text content for backwards compat
        Assert.Single(result.Content);
        var textBlock = Assert.IsType<TextContentBlock>(result.Content[0]);
        Assert.Contains("Seattle", textBlock.Text);

        // And structured content for modern clients
        Assert.NotNull(result.StructuredContent);
        var structured = result.StructuredContent.ToString();
        Assert.Contains("Seattle", structured);
        Assert.Contains("72", structured);
        Assert.Contains("Sunny", structured);
    }

    // ── Metadata & fluent tools ─────────────────────────────────────────────

    [Fact]
    public async Task MetadataAttributeTool_ReturnsResponse()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "MetadataAttributeTool", new { });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("Metadata attribute tool response", response);
    }

    [Fact]
    public async Task FluentMetadataTool_ReturnsResponse()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "FluentMetadataTool", new { });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("Fluent metadata tool response", response);
    }

    [Fact]
    public async Task FluentDefinedTool_WithProperties_ReturnsFormattedResponse()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "FluentDefinedTool", new { city = "Seattle", zipCode = "98101" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("Seattle", response);
        Assert.Contains("98101", response);
    }

    // ── MCP App tools ───────────────────────────────────────────────────────

    [Fact]
    public async Task HelloApp_ReturnsResponse()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "HelloApp", new { });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("HelloApp", response);
    }

    [Fact]
    public async Task MinimalApp_ReturnsResponse()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "MinimalApp", new { });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("MinimalApp", response);
    }

    // ── Input schema tools ────────────────────────────────────────────────

    [Fact]
    public async Task InputSchemaTool_WithAllArguments_ReturnsExpectedResponse()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "InputSchemaTool", new { location = "Seattle, WA", units = "fahrenheit" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("Seattle, WA", response);
        Assert.Contains("fahrenheit", response);
    }

    [Fact]
    public async Task InputSchemaTool_WithRequiredOnly_ReturnsDefaultUnits()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "InputSchemaTool", new { location = "Portland, OR" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("Portland, OR", response);
        Assert.Contains("celsius", response);
    }

    [Fact]
    public async Task InputSchemaTool_WithoutRequiredLocation_ReturnsError()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "InputSchemaTool", new { units = "fahrenheit" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.Contains("error", response);
        Assert.Contains("location", response);
    }

    // ── Edge cases ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InvalidTool_ReturnsError()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "NonExistentTool", new { someParam = "test" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.Contains("error", response);
        Assert.Contains("Unknown tool", response);
    }

    [Fact]
    public async Task EchoTool_WithExtraParameters_StillSucceeds()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "EchoTool", new { message = "hello", unknownParam = "ignored" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("hello", response);
    }

    [Fact]
    public async Task EchoTool_WithEmptyString_ReturnsEmpty()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "EchoTool", new { message = "" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
    }

    [Fact]
    public async Task MultipleSequentialRequests_AllSucceed()
    {
        for (int i = 1; i <= 3; i++)
        {
            var request = CallToolHelper.CreateToolCallRequest(i, "EchoTool",
                new { message = $"sequential-message-{i}" });

            var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);
            Assert.NotNull(response);
            Assert.Contains($"sequential-message-{i}", response);
        }
    }

    private static string GetTestDataFilePath(string fileName)
    {
        var assemblyLocation = Path.GetDirectoryName(typeof(CallToolTests).Assembly.Location)
            ?? throw new InvalidOperationException("Could not determine assembly location");

        var testDataPath = Path.Combine(assemblyLocation, "TestData", fileName);

        if (!File.Exists(testDataPath))
        {
            throw new FileNotFoundException($"Test data file not found at: {testDataPath}", fileName);
        }

        return testDataPath;
    }
}
