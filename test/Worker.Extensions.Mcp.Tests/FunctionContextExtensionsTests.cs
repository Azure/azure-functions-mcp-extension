using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Moq;
using System.Collections.Immutable;

namespace Worker.Extensions.Mcp.Tests;

public class FunctionContextExtensionsTests
{
    [Fact]
    public void TryGetToolInvocationContext_ReturnsTrue_WhenContextExists()
    {
        // Arrange
        var mockContext = new Mock<FunctionContext>();
        var expectedToolContext = new ToolInvocationContext { Name = "TestTool" };

        var items = new Dictionary<object, object>
        {
            { Constants.ToolInvocationContextKey, expectedToolContext }
        };

        mockContext.Setup(c => c.Items).Returns(items);

        // Act
        var result = mockContext.Object.TryGetToolInvocationContext(out var actualToolContext);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedToolContext, actualToolContext);
    }

    [Fact]
    public void TryGetToolInvocationContext_ReturnsFalse_WhenContextMissing()
    {
        // Arrange
        var mockContext = new Mock<FunctionContext>();
        var items = new Dictionary<object, object>(); // empty dictionary

        mockContext.Setup(c => c.Items).Returns(items);

        // Act
        var result = mockContext.Object.TryGetToolInvocationContext(out var toolContext);

        // Assert
        Assert.False(result);
        Assert.Null(toolContext);
    }

    [Fact]
    public void TryGetToolInvocationContext_ReturnsFalse_WhenContextIsWrongType()
    {
        // Arrange
        var mockContext = new Mock<FunctionContext>();
        var items = new Dictionary<object, object>
        {
            { Constants.ToolInvocationContextKey, "NotAToolInvocationContext" }
        };

        mockContext.Setup(c => c.Items).Returns(items);

        // Act
        var result = mockContext.Object.TryGetToolInvocationContext(out var toolContext);

        // Assert
        Assert.False(result);
        Assert.Null(toolContext);
    }

    [Fact]
    public void TryGetMcpToolTriggerName_ReturnsTrue_WhenTriggerBindingIsPresent()
    {
        var bindingMetadataMock = new Mock<BindingMetadata>();
        bindingMetadataMock.Setup(b => b.Type).Returns(Constants.McpToolTriggerBindingType);
        bindingMetadataMock.Setup(b=>b.Name).Returns("myTrigger");

        var functionDefinitionMock = new Mock<FunctionDefinition>();
        functionDefinitionMock.Setup(f => f.InputBindings)
            .Returns(ImmutableDictionary.Create<string, BindingMetadata>().Add("myTrigger", bindingMetadataMock.Object));

        var contextMock = new Mock<FunctionContext>();
        contextMock.Setup(c => c.FunctionDefinition).Returns(functionDefinitionMock.Object);

        var result = contextMock.Object.TryGetMcpToolTriggerName(out var triggerName);

        Assert.True(result);
        Assert.Equal("myTrigger", triggerName);
    }
}
