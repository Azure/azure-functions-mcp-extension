// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;
using Worker.Extensions.Mcp.Tests.Helpers;

namespace Worker.Extensions.Mcp.Tests.ConverterTests;

public class PromptInvocationContextConverterTests
{
    [Fact]
    public async Task ConvertAsync_ValidPromptInvocationContext_ReturnsSuccess()
    {
        var converter = new PromptInvocationContextConverter();
        var promptContext = new PromptInvocationContext
        {
            Name = "code_review",
            Arguments = new Dictionary<string, string> { { "code", "print('hello')" } }
        };

        var functionContext = FunctionContextHelper.CreateFunctionContextWithPromptContext(promptContext);
        var context = ConverterContextHelper.CreateConverterContext(typeof(PromptInvocationContext), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        var returned = Assert.IsType<PromptInvocationContext>(result.Value);
        Assert.Equal("code_review", returned.Name);
        Assert.Equal("print('hello')", returned.Arguments?["code"]);
    }

    [Fact]
    public async Task ConvertAsync_WrongTargetType_ReturnsUnhandled()
    {
        var converter = new PromptInvocationContextConverter();
        var functionContext = FunctionContextHelper.CreateEmptyFunctionContext();
        var context = ConverterContextHelper.CreateConverterContext(typeof(string), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Unhandled, result.Status);
    }

    [Fact]
    public async Task ConvertAsync_MissingPromptContext_ReturnsFailed()
    {
        var converter = new PromptInvocationContextConverter();
        var functionContext = FunctionContextHelper.CreateEmptyFunctionContext();
        var context = ConverterContextHelper.CreateConverterContext(typeof(PromptInvocationContext), null, functionContext);

        var result = await converter.ConvertAsync(context);

        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.IsType<InvalidOperationException>(result.Error);
        Assert.Contains(nameof(PromptInvocationContext), result.Error!.Message);
    }

    [Fact]
    public async Task ConvertAsync_NullContext_ThrowsArgumentNullException()
    {
        var converter = new PromptInvocationContextConverter();

        await Assert.ThrowsAsync<ArgumentNullException>(() => converter.ConvertAsync(null!).AsTask());
    }
}
