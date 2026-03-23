// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;
using Worker.Extensions.Mcp.Tests.Helpers;

namespace Worker.Extensions.Mcp.Tests.ConverterTests;

public class PromptArgumentConverterTests
{
    [Fact]
    public async Task ConvertAsync_ValidPromptArgument_ReturnsSuccess()
    {
        var converter = new PromptArgumentConverter();
        var promptContext = new PromptInvocationContext
        {
            Name = "code_review",
            Arguments = new Dictionary<string, string>
            {
                ["code"] = "print('hello')",
                ["language"] = "python"
            }
        };

        var functionContext = FunctionContextHelper.CreateFunctionContextWithPromptContext(promptContext);
        var attr = new McpPromptArgumentAttribute("code", "The code to review", isRequired: true);
        var context = ConverterContextHelper.CreateConverterContext(typeof(string), attr, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        Assert.Equal("print('hello')", result.Value);
    }

    [Fact]
    public async Task ConvertAsync_MissingArgument_ReturnsUnhandled()
    {
        var converter = new PromptArgumentConverter();
        var promptContext = new PromptInvocationContext
        {
            Name = "code_review",
            Arguments = new Dictionary<string, string> { ["code"] = "print('hello')" }
        };

        var functionContext = FunctionContextHelper.CreateFunctionContextWithPromptContext(promptContext);
        var attr = new McpPromptArgumentAttribute("nonexistent", "Does not exist");
        var context = ConverterContextHelper.CreateConverterContext(typeof(string), attr, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_NoPromptContext_ReturnsUnhandled()
    {
        var converter = new PromptArgumentConverter();
        var functionContext = FunctionContextHelper.CreateEmptyFunctionContext();
        var attr = new McpPromptArgumentAttribute("code", "The code");
        var context = ConverterContextHelper.CreateConverterContext(typeof(string), attr, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_NullArguments_ReturnsUnhandled()
    {
        var converter = new PromptArgumentConverter();
        var promptContext = new PromptInvocationContext
        {
            Name = "code_review",
            Arguments = null
        };

        var functionContext = FunctionContextHelper.CreateFunctionContextWithPromptContext(promptContext);
        var attr = new McpPromptArgumentAttribute("code", "The code");
        var context = ConverterContextHelper.CreateConverterContext(typeof(string), attr, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_NullContext_ThrowsArgumentNullException()
    {
        var converter = new PromptArgumentConverter();

        await Assert.ThrowsAsync<ArgumentNullException>(() => converter.ConvertAsync(null!).AsTask());
    }
}
