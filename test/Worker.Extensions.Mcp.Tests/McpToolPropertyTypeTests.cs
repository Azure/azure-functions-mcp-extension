// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Builder;

namespace Worker.Extensions.Mcp.Tests;

public class McpToolPropertyTypeTests
{
    [Fact]
    public void StaticProperties_ReturnExpectedValues()
    {
        Assert.Equal("string", McpToolPropertyType.String.TypeName);
        Assert.False(McpToolPropertyType.String.IsArray);

        Assert.Equal("number", McpToolPropertyType.Number.TypeName);
        Assert.False(McpToolPropertyType.Number.IsArray);

        Assert.Equal("integer", McpToolPropertyType.Integer.TypeName);
        Assert.False(McpToolPropertyType.Integer.IsArray);

        Assert.Equal("boolean", McpToolPropertyType.Boolean.TypeName);
        Assert.False(McpToolPropertyType.Boolean.IsArray);

        Assert.Equal("object", McpToolPropertyType.Object.TypeName);
        Assert.False(McpToolPropertyType.Object.IsArray);
    }

    [Fact]
    public void StaticArrayProperties_ReturnExpectedValues()
    {
        Assert.Equal("string", McpToolPropertyType.StringArray.TypeName);
        Assert.True(McpToolPropertyType.StringArray.IsArray);

        Assert.Equal("number", McpToolPropertyType.NumberArray.TypeName);
        Assert.True(McpToolPropertyType.NumberArray.IsArray);

        Assert.Equal("integer", McpToolPropertyType.IntegerArray.TypeName);
        Assert.True(McpToolPropertyType.IntegerArray.IsArray);

        Assert.Equal("boolean", McpToolPropertyType.BooleanArray.TypeName);
        Assert.True(McpToolPropertyType.BooleanArray.IsArray);

        Assert.Equal("object", McpToolPropertyType.ObjectArray.TypeName);
        Assert.True(McpToolPropertyType.ObjectArray.IsArray);
    }

    [Fact]
    public void StaticProperties_AreSingletonInstances()
    {
        Assert.Same(McpToolPropertyType.String, McpToolPropertyType.String);
        Assert.Same(McpToolPropertyType.Number, McpToolPropertyType.Number);
        Assert.Same(McpToolPropertyType.Integer, McpToolPropertyType.Integer);
        Assert.Same(McpToolPropertyType.Boolean, McpToolPropertyType.Boolean);
        Assert.Same(McpToolPropertyType.Object, McpToolPropertyType.Object);

        Assert.Same(McpToolPropertyType.StringArray, McpToolPropertyType.StringArray);
        Assert.Same(McpToolPropertyType.NumberArray, McpToolPropertyType.NumberArray);
        Assert.Same(McpToolPropertyType.IntegerArray, McpToolPropertyType.IntegerArray);
        Assert.Same(McpToolPropertyType.BooleanArray, McpToolPropertyType.BooleanArray);
        Assert.Same(McpToolPropertyType.ObjectArray, McpToolPropertyType.ObjectArray);
    }

    [Fact]
    public void Equality_ByValue_NotByReference()
    {
        var expected = new McpToolPropertyType("string");
        Assert.Equal(expected, McpToolPropertyType.String);
        Assert.NotSame(expected, McpToolPropertyType.String);

        var expectedArray = new McpToolPropertyType("string", true);
        Assert.Equal(expectedArray, McpToolPropertyType.StringArray);
        Assert.NotSame(expectedArray, McpToolPropertyType.StringArray);
    }

    [Fact]
    public void AsArray_OnNonArray_ReturnsArrayWithSameTypeName()
    {
        var original = McpToolPropertyType.Integer;
        var arrayVersion = original.AsArray();

        Assert.Equal(original.TypeName, arrayVersion.TypeName);
        Assert.True(arrayVersion.IsArray);
        Assert.NotSame(original, arrayVersion);
        Assert.Equal(new McpToolPropertyType("integer", true), arrayVersion);
    }

    [Fact]
    public void AsArray_OnArray_ReturnsNewArrayInstance()
    {
        var arrayOriginal = McpToolPropertyType.IntegerArray;
        var arrayAgain = arrayOriginal.AsArray();

        Assert.True(arrayOriginal.IsArray);
        Assert.True(arrayAgain.IsArray);
        Assert.Equal(arrayOriginal.TypeName, arrayAgain.TypeName);
        Assert.NotSame(arrayOriginal, arrayAgain); // new instance each call
        Assert.Equal(arrayOriginal, arrayAgain);   // value equality
    }

    [Theory]
    [InlineData("string", false)]
    [InlineData("string", true)]
    [InlineData("number", false)]
    [InlineData("number", true)]
    [InlineData("integer", false)]
    [InlineData("integer", true)]
    [InlineData("boolean", false)]
    [InlineData("boolean", true)]
    [InlineData("object", false)]
    [InlineData("object", true)]
    public void NewInstances_WithSameValues_AreValueEqual(string typeName, bool isArray)
    {
        var a = new McpToolPropertyType(typeName, isArray);
        var b = new McpToolPropertyType(typeName, isArray);

        Assert.Equal(a, b);
        Assert.NotSame(a, b);
    }
}
