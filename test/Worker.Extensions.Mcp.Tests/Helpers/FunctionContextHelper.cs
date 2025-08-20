
using System.Collections.Immutable;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Moq;

namespace Worker.Extensions.Mcp.Tests.Helpers;

public static class FunctionContextHelper
{
    public static FunctionContext CreateFunctionContext(
        string triggerName,
        Dictionary<string, object> bindingData,
        out Dictionary<object, object?> items)
    {
        var binding = CreateBindingMetadata(triggerName);
        return CreateFunctionContext([binding], bindingData, out items);
    }

    public static FunctionContext CreateFunctionContext(
        ImmutableArray<BindingMetadata> bindings,
        Dictionary<string, object> bindingData,
        out Dictionary<object, object?> items)
    {
        items = [];

        var functionDefinitionMock = new Mock<FunctionDefinition>();
        functionDefinitionMock.SetupGet(f => f.InputBindings)
            .Returns(bindings.ToImmutableDictionary(b => b.Name, b => b));

        var bindingContextMock = new Mock<BindingContext>();
        bindingContextMock.SetupGet(b => b.BindingData).Returns(bindingData!);

        var contextMock = new Mock<FunctionContext>();
        contextMock.SetupGet(c => c.FunctionDefinition).Returns(functionDefinitionMock.Object);
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

    public static BindingMetadata CreateBindingMetadata(string name, string type = Constants.McpToolTriggerBindingType)
    {
        var bindingMetadataMock = new Mock<BindingMetadata>();
        bindingMetadataMock.SetupGet(b => b.Name).Returns(name);
        bindingMetadataMock.SetupGet(b => b.Type).Returns(type);

        return bindingMetadataMock.Object;
    }
}
