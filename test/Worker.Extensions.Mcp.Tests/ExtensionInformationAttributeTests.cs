// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace Worker.Extensions.Mcp.Tests;

public class ExtensionInformationAttributeTests
{
    [Fact]
    public void ExtensionInformationAttribute_IsPresent()
    {
        var assembly = typeof(McpToolTriggerAttribute).Assembly;
        var attribute = assembly.GetCustomAttribute<ExtensionInformationAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("Microsoft.Azure.Functions.Extensions.Mcp", attribute.ExtensionPackage);
    }
}
