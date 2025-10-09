// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Executors;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;
using System.Text.Json;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;
public class McpToolListenerTests
{
    private static IMcpToolProperty CreateProperty(string name, bool required)
    {
        var mock = new Mock<IMcpToolProperty>();
        mock.SetupAllProperties();
        mock.Object.PropertyName = name;
        mock.Object.IsRequired = required;
        return mock.Object;
    }

    private static RequestContext<CallToolRequestParams> CreateRequest(params (string key, JsonElement value)[] args)
    {
        var dict = args?.ToDictionary(x => x.key, x => x.value) ?? new Dictionary<string, JsonElement>();
        var server = new Mock<IMcpServer>().Object;
        var parameters = new CallToolRequestParams { Name = "params", Arguments = dict };

        return new RequestContext<CallToolRequestParams>(server)
        {
            Params = parameters
        };
    }

    [Fact]
    public async Task RunAsync_Throws_WhenRequiredPropertyMissing()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var properties = new[] { CreateProperty("foo", true) };
        var listener = new McpToolListener(executor, "func", "tool", null, properties);

        var request = CreateRequest(); // No arguments

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            listener.RunAsync(request, CancellationToken.None));
        Assert.Contains("One or more required tool properties are missing values. Please provide: foo", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public async Task RunAsync_Throws_WhenRequiredPropertyIsNull()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var properties = new[] { CreateProperty("foo", true) };
        var listener = new McpToolListener(executor, "func", "tool", null, properties);

        var request = CreateRequest(("foo", JsonDocument.Parse("null").RootElement));

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            listener.RunAsync(request, CancellationToken.None));
        Assert.Contains("One or more required tool properties are missing values. Please provide: foo", ex.Message);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_Throws_WhenRequiredPropertyMissing()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(); // No arguments

        var ex = Assert.Throws<McpException>(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params));
        Assert.Contains("One or more required tool properties are missing values. Please provide: foo", ex.Message);
        Assert.Equal(McpErrorCode.InvalidParams, ex.ErrorCode);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_Throws_WhenRequiredPropertyIsNull()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("null").RootElement));

        var ex = Assert.Throws<McpException>(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params));
        Assert.Contains("One or more required tool properties are missing values. Please provide: foo", ex.Message);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_Throws_WhenRequiredPropertyIsEmptyString()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("\"\"").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_DoesNotThrow_WhenRequiredPropertyPresentAndValid()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("\"bar\"").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_Throws_WhenRequiredPropertyIsEmptyArray()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("[]").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_DoesNotThrow_WhenRequiredPropertyIsNonEmptyArray()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("[1]").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_Throws_WhenRequiredPropertyIsEmptyObject()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("{}").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_DoesNotThrow_WhenRequiredPropertyIsNonEmptyObject()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("{\"bar\":1}").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateArgumentsHaveRequiredProperties_DoesNotThrow_WhenRequiredPropertyIsNumber()
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse("123").RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void ValidateArgumentsHaveRequiredProperties_DoesNotThrow_WhenRequiredPropertyIsBoolean(string boolValue)
    {
        var properties = new[] { CreateProperty("foo", true) };
        var request = CreateRequest(("foo", JsonDocument.Parse(boolValue).RootElement));

        var ex = Record.Exception(() =>
            McpToolListener.ValidateArgumentsHaveRequiredProperties(properties, request.Params));
        Assert.Null(ex);
    }
}
