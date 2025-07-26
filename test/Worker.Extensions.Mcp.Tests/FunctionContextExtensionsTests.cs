using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Moq;

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
    public void TryGetMcpToolTriggerName_ReturnsTrue_WhenAttributeExists()
    {
        // Arrange
        var parameter = new FunctionParameter(
            name: "myTrigger",
            type: typeof(string),
            properties: new Dictionary<string, object>
            {
                { "bindingAttribute", new McpToolTriggerAttribute("TestTool", null) }
            }
        );

        var functionDefinitionMock = new Mock<FunctionDefinition>();
        functionDefinitionMock.Setup(f => f.Parameters)
            .Returns([parameter]);

        var contextMock = new Mock<FunctionContext>();
        contextMock.Setup(c => c.FunctionDefinition).Returns(functionDefinitionMock.Object);

        // Act
        var result = contextMock.Object.TryGetMcpToolTriggerName(out var triggerName);

        // Assert
        Assert.True(result);
        Assert.Equal("myTrigger", triggerName);
    }

    [Fact]
    public void TryGetMcpToolTriggerName_ReturnsFalse_WhenAttributeIsMissing()
    {
        // Arrange
        var parameter = new FunctionParameter(
            name: "myTrigger",
            type: typeof(string)
        );

        var functionDefinitionMock = new Mock<FunctionDefinition>();
        functionDefinitionMock.Setup(f => f.Parameters)
            .Returns([parameter]);

        var contextMock = new Mock<FunctionContext>();
        contextMock.Setup(c => c.FunctionDefinition).Returns(functionDefinitionMock.Object);

        // Act
        var result = contextMock.Object.TryGetMcpToolTriggerName(out var triggerName);

        // Assert
        Assert.False(result);
        Assert.Equal(string.Empty, triggerName);
    }
}
