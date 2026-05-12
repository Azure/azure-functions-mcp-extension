// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ToolTests;

/// <summary>
/// End-to-end coverage for tool invocation. Per-tool input/output binding,
/// argument conversion, schema validation, and content rendering are
/// covered by unit tests:
/// - Input binding: ToolInvocationPocoConverterTests,
///   ToolInvocationArgumentTypeConverterTests, ToolInvocationContextConverterTests
/// - Type/collection conversion: McpInputConversionHelperTests,
///   McpInputConversionHelperCollectionTests
/// - Output binding: ToolReturnValueBinderTests, SimpleToolReturnValueBinderTests
/// - Schema / required args / unknown tool: McpToolSchemaValidatorTests,
///   DefaultToolRegistryTests, McpToolListenerTests
///
/// What we keep here is one representative test per integration "shape":
/// simple invocation, typed args, POCO + structured content, binary content,
/// session reuse across sequential calls, and the unknown-tool error path.
/// </summary>
public class CallToolTests(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    : McpE2ETestBase(fixture, testOutputHelper)
{
    private Uri AppRootEndpoint => Fixture.AppRootEndpoint ?? throw new InvalidOperationException("AppRootEndpoint is not set.");

    [Fact]
    public async Task SimpleTool_Invocation_ReturnsExpectedContent()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "EchoTool", new { message = "hello-world" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);
        Assert.Contains("hello-world", response);
    }

    [Fact]
    public async Task TypedArguments_AreBoundAndReturned()
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
    public async Task PocoOutputTool_ReturnsStructuredContent()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "PocoOutputTool", new { city = "Seattle" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);

        var result = CallToolResultHelper.ParseSseToCallToolResult(response, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(result);

        var textBlock = Assert.IsType<TextContentBlock>(Assert.Single(result.Content));
        Assert.Contains("Seattle", textBlock.Text);

        Assert.NotNull(result.StructuredContent);
        var structured = result.StructuredContent.ToString();
        Assert.Contains("Seattle", structured);
        Assert.Contains("Sunny", structured);
    }

    [Fact]
    public async Task ImageContentTool_ReturnsBinaryContentBlock()
    {
        var imageDataPath = GetTestDataFilePath("image-base64.txt");
        var data = await File.ReadAllTextAsync(imageDataPath, TestContext.Current.CancellationToken);

        var request = CallToolHelper.CreateToolCallRequest(1, "ImageContentTool", new { data, mimeType = "image/jpeg" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.NotNull(response);

        var result = CallToolResultHelper.ParseSseToCallToolResult(response, McpJsonUtilities.DefaultOptions);
        Assert.NotNull(result);
        var imageBlock = Assert.IsType<ImageContentBlock>(Assert.Single(result.Content));
        Assert.Equal("image/jpeg", imageBlock.MimeType);
        Assert.StartsWith(data.Substring(0, 20), Encoding.UTF8.GetString(imageBlock.Data.Span));
    }

    [Fact]
    public async Task UnknownTool_ReturnsError()
    {
        var request = CallToolHelper.CreateToolCallRequest(1, "NonExistentTool", new { someParam = "test" });
        var response = await CallToolHelper.MakeToolCallRequest(AppRootEndpoint, request, TestOutputHelper);

        Assert.Contains("error", response);
        Assert.Contains("Unknown tool", response);
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
