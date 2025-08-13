
using System.Collections.Immutable;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Moq;

namespace Worker.Extensions.Mcp.Tests.Helpers;

public static class FunctionContextHelper
{
    public static FunctionContext CreateFunctionContext(
        string triggerName,
        McpToolTriggerAttribute attribute,
        Dictionary<string, object> bindingData,
        out Dictionary<object, object?> items)
    {
        var parameter = CreateFunctionParameter(triggerName, attribute);
        return CreateFunctionContext([parameter], bindingData, out items);
    }

    public static FunctionContext CreateFunctionContext(
        ImmutableArray<FunctionParameter> parameters,
        Dictionary<string, object> bindingData,
        out Dictionary<object, object?> items)
    {
        items = [];

        var funcDefMock = new Mock<FunctionDefinition>();
        funcDefMock.SetupGet(f => f.Parameters).Returns(parameters);

        var bindingContextMock = new Mock<BindingContext>();
        bindingContextMock.SetupGet(b => b.BindingData).Returns(bindingData!);

        var contextMock = new Mock<FunctionContext>();
        contextMock.SetupGet(c => c.FunctionDefinition).Returns(funcDefMock.Object);
        contextMock.SetupGet(c => c.BindingContext).Returns(bindingContextMock.Object);
        contextMock.SetupGet(c => c.Items).Returns(items!);

        return contextMock.Object;
    }

    public static FunctionContext CreateFunctionContextWithToolContext(ToolInvocationContext toolContext)
    {
        var items = new Dictionary<object, object?>
        {
            { Constants.ToolInvocationContextKey, toolContext }
        };

        var contextMock = new Mock<FunctionContext>();
        contextMock.SetupGet(c => c.Items).Returns(items!);

        // Other members can be left unmocked unless needed
        return contextMock.Object;
    }

    public static FunctionContext CreateEmptyFunctionContext()
    {
        var contextMock = new Mock<FunctionContext>();
        contextMock.SetupGet(c => c.Items).Returns(new Dictionary<object, object?>()!);
        return contextMock.Object;
    }

    private static FunctionParameter CreateFunctionParameter(string name, object? bindingAttribute)
    {
        var props = bindingAttribute is null
            ? []
            : new Dictionary<string, object> { ["bindingAttribute"] = bindingAttribute };

        return new FunctionParameter(name, typeof(string), props);
    }
}
