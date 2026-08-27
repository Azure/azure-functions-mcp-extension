// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Tests;

public class McpToolTriggerAttributeTests
{
    [Fact]
    public void Constructor_AnnotationPropertiesUseProtocolDefaults()
    {
        var attribute = new McpToolTriggerAttribute("TestTool", "Test description");

        Assert.Equal("TestTool", attribute.ToolName);
        Assert.Equal("Test description", attribute.Description);
        Assert.Null(attribute.Title);
        Assert.False(attribute.ReadOnly);
        Assert.False(attribute.Destructive);
    }

    [Fact]
    public void AnnotationProperties_CanBeConfigured()
    {
        var attribute = new McpToolTriggerAttribute("TestTool")
        {
            Title = "Test Tool",
            ReadOnly = true,
            Destructive = true
        };

        Assert.Equal("Test Tool", attribute.Title);
        Assert.True(attribute.ReadOnly);
        Assert.True(attribute.Destructive);
    }
}
