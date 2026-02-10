// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests.Configuration;

public class ToolPropertyParserTests
{
    [Fact]
    public void GetPropertiesJson_EmptyList_ReturnsEmptyArray()
    {
        var result = ToolPropertyParser.GetPropertiesJson([]);

        Assert.Equal("[]", result!.ToString());
    }

    [Fact]
    public void GetPropertiesJson_WithProperties_ReturnsJsonArray()
    {
        var properties = new List<ToolProperty>
        {
            new("name", "string", "The name", true),
            new("count", "integer", "The count", false)
        };

        var result = ToolPropertyParser.GetPropertiesJson(properties);
        var json = result!.ToString();

        Assert.Contains("\"propertyName\":\"name\"", json);
        Assert.Contains("\"propertyName\":\"count\"", json);
        Assert.Contains("\"propertyType\":\"string\"", json);
        Assert.Contains("\"propertyType\":\"integer\"", json);
    }
}
