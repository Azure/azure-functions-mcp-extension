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

    [Fact]
    public void Equality_WithNullEnumValues_AreEqual()
    {
        var type1 = new McpToolPropertyType("string", false, null);
        var type2 = new McpToolPropertyType("string", false, null);

        Assert.Equal(type1, type2);
        Assert.True(type1 == type2);
        Assert.False(type1 != type2);
        Assert.Equal(type1.GetHashCode(), type2.GetHashCode());
    }

    [Fact]
    public void Equality_WithEmptyEnumValues_AreEqual()
    {
        var emptyList1 = new List<string>();
        var emptyList2 = new List<string>();

        var type1 = new McpToolPropertyType("string", false, emptyList1);
        var type2 = new McpToolPropertyType("string", false, emptyList2);

        Assert.Equal(type1, type2);
        Assert.True(type1 == type2);
        Assert.False(type1 != type2);
        Assert.Equal(type1.GetHashCode(), type2.GetHashCode());
    }

    [Fact]
    public void Equality_WithSameEnumValues_DifferentArrayInstances_AreEqual()
    {
        // This is the key test case you mentioned - same values, different array instances
        var enumValues1 = new[] { "Active", "Inactive", "Pending" };
        var enumValues2 = new[] { "Active", "Inactive", "Pending" };

        var type1 = new McpToolPropertyType("string", false, enumValues1);
        var type2 = new McpToolPropertyType("string", false, enumValues2);

        // These should be equal even though the arrays are different instances
        Assert.NotSame(enumValues1, enumValues2); // Different array instances
        Assert.Equal(type1, type2);
        Assert.True(type1 == type2);
        Assert.False(type1 != type2);
        Assert.Equal(type1.GetHashCode(), type2.GetHashCode());
    }

    [Fact]
    public void Equality_WithSameEnumValues_DifferentCollectionTypes_AreEqual()
    {
        var enumArray = new[] { "Low", "Medium", "High" };
        var enumList = new List<string> { "Low", "Medium", "High" };

        var type1 = new McpToolPropertyType("string", false, enumArray);
        var type2 = new McpToolPropertyType("string", false, enumList);

        Assert.Equal(type1, type2);
        Assert.True(type1 == type2);
        Assert.False(type1 != type2);
        Assert.Equal(type1.GetHashCode(), type2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentEnumValues_AreNotEqual()
    {
        var enumValues1 = new[] { "Active", "Inactive", "Pending" };
        var enumValues2 = new[] { "Active", "Inactive", "Complete" }; // Different last value

        var type1 = new McpToolPropertyType("string", false, enumValues1);
        var type2 = new McpToolPropertyType("string", false, enumValues2);

        Assert.NotEqual(type1, type2);
        Assert.False(type1 == type2);
        Assert.True(type1 != type2);
        Assert.NotEqual(type1.GetHashCode(), type2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentEnumValueOrder_AreNotEqual()
    {
        var enumValues1 = new[] { "Active", "Inactive", "Pending" };
        var enumValues2 = new[] { "Inactive", "Active", "Pending" }; // Different order

        var type1 = new McpToolPropertyType("string", false, enumValues1);
        var type2 = new McpToolPropertyType("string", false, enumValues2);

        Assert.NotEqual(type1, type2);
        Assert.False(type1 == type2);
        Assert.True(type1 != type2);
        Assert.NotEqual(type1.GetHashCode(), type2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentEnumValueCount_AreNotEqual()
    {
        var enumValues1 = new[] { "Active", "Inactive" };
        var enumValues2 = new[] { "Active", "Inactive", "Pending" }; // Different count

        var type1 = new McpToolPropertyType("string", false, enumValues1);
        var type2 = new McpToolPropertyType("string", false, enumValues2);

        Assert.NotEqual(type1, type2);
        Assert.False(type1 == type2);
        Assert.True(type1 != type2);
        Assert.NotEqual(type1.GetHashCode(), type2.GetHashCode());
    }

    [Fact]
    public void Equality_OneNullOneEmptyEnumValues_AreNotEqual()
    {
        var type1 = new McpToolPropertyType("string", false, null);
        var type2 = new McpToolPropertyType("string", false, Array.Empty<string>());

        Assert.NotEqual(type1, type2);
        Assert.False(type1 == type2);
        Assert.True(type1 != type2);
    }

    [Fact]
    public void Equality_OneNullOnePopulatedEnumValues_AreNotEqual()
    {
        var type1 = new McpToolPropertyType("string", false, null);
        var type2 = new McpToolPropertyType("string", false, new[] { "Active" });

        Assert.NotEqual(type1, type2);
        Assert.False(type1 == type2);
        Assert.True(type1 != type2);
    }

    [Fact]
    public void Equality_WithEnumValues_IgnoresOtherProperties()
    {
        var enumValues = new[] { "Active", "Inactive" };

        // Same enum values but different TypeName
        var type1 = new McpToolPropertyType("string", false, enumValues);
        var type2 = new McpToolPropertyType("number", false, enumValues);

        Assert.NotEqual(type1, type2);

        // Same enum values but different IsArray
        var type3 = new McpToolPropertyType("string", false, enumValues);
        var type4 = new McpToolPropertyType("string", true, enumValues);

        Assert.NotEqual(type3, type4);
    }

    [Fact]
    public void HashCode_Consistency_SameObjectMultipleCalls()
    {
        var enumValues = new[] { "Value1", "Value2", "Value3" };
        var type = new McpToolPropertyType("string", false, enumValues);

        var hash1 = type.GetHashCode();
        var hash2 = type.GetHashCode();
        var hash3 = type.GetHashCode();

        Assert.Equal(hash1, hash2);
        Assert.Equal(hash2, hash3);
    }

    [Fact]
    public void HashCode_EqualObjects_SameHashCode()
    {
        var enumValues1 = new[] { "A", "B", "C" };
        var enumValues2 = new[] { "A", "B", "C" }; // Same values, different array

        var type1 = new McpToolPropertyType("string", false, enumValues1);
        var type2 = new McpToolPropertyType("string", false, enumValues2);

        Assert.Equal(type1, type2);
        Assert.Equal(type1.GetHashCode(), type2.GetHashCode());
    }

    [Fact]
    public void HashCode_DifferentEnumValues_DifferentHashCodes()
    {
        var type1 = new McpToolPropertyType("string", false, new[] { "A", "B" });
        var type2 = new McpToolPropertyType("string", false, new[] { "A", "C" });

        Assert.NotEqual(type1, type2);
        // Hash codes should be different (though technically not required by contract)
        Assert.NotEqual(type1.GetHashCode(), type2.GetHashCode());
    }

    [Fact]
    public void IsEnum_WithNullEnumValues_ReturnsFalse()
    {
        var type = new McpToolPropertyType("string", false, null);
        Assert.False(type.IsEnum);
    }

    [Fact]
    public void IsEnum_WithEmptyEnumValues_ReturnsFalse()
    {
        var type = new McpToolPropertyType("string", false, Array.Empty<string>());
        Assert.False(type.IsEnum);
    }

    [Fact]
    public void IsEnum_WithPopulatedEnumValues_ReturnsTrue()
    {
        var type = new McpToolPropertyType("string", false, new[] { "Value1", "Value2" });
        Assert.True(type.IsEnum);
    }

    [Fact]
    public void IsEnum_WithSingleEnumValue_ReturnsTrue()
    {
        var type = new McpToolPropertyType("string", false, new[] { "OnlyValue" });
        Assert.True(type.IsEnum);
    }

    [Fact]
    public void AsArray_WithEnumValues_PreservesEnumValues()
    {
        var enumValues = new[] { "Active", "Inactive", "Pending" };
        var original = new McpToolPropertyType("string", false, enumValues);
        var arrayVersion = original.AsArray();

        Assert.Equal(original.TypeName, arrayVersion.TypeName);
        Assert.True(arrayVersion.IsArray);
        Assert.Equal(enumValues, arrayVersion.EnumValues);
        Assert.True(arrayVersion.IsEnum);
    }

    [Fact]
    public void AsArray_OnEnumType_ReturnsNewInstanceWithSameEnumValues()
    {
        var enumValues = new[] { "Low", "Medium", "High" };
        var enumType = new McpToolPropertyType("string", false, enumValues);
        var enumArrayType = enumType.AsArray();

        Assert.Equal("string", enumArrayType.TypeName);
        Assert.True(enumArrayType.IsArray);
        Assert.Equal(enumValues, enumArrayType.EnumValues);
        Assert.True(enumArrayType.IsEnum);
        Assert.NotSame(enumType, enumArrayType);
    }

    [Fact]
    public void JobTypeEnum_EqualityScenario()
    {
        // This represents the real-world scenario mentioned in the issue
        var jobTypeValues = new[] { "FullTime", "PartTime", "Contract", "Internship", "Temporary", "Freelance", "Unemployed" };
        
        // Two property types created at different times/places with same job type values
        var jobType1 = new McpToolPropertyType("string", false, new[] { "FullTime", "PartTime", "Contract", "Internship", "Temporary", "Freelance", "Unemployed" });
        var jobType2 = new McpToolPropertyType("string", false, jobTypeValues);

        Assert.Equal(jobType1, jobType2);
        Assert.Equal(jobType1.GetHashCode(), jobType2.GetHashCode());
        Assert.True(jobType1 == jobType2);
    }

    [Fact]
    public void JobTypeEnum_ArrayVersion_EqualityScenario()
    {
        var jobTypeValues1 = new[] { "FullTime", "PartTime", "Contract", "Internship", "Temporary", "Freelance", "Unemployed" };
        var jobTypeValues2 = new[] { "FullTime", "PartTime", "Contract", "Internship", "Temporary", "Freelance", "Unemployed" };
        
        var jobTypeArray1 = new McpToolPropertyType("string", true, jobTypeValues1);
        var jobTypeArray2 = new McpToolPropertyType("string", true, jobTypeValues2);

        Assert.Equal(jobTypeArray1, jobTypeArray2);
        Assert.Equal(jobTypeArray1.GetHashCode(), jobTypeArray2.GetHashCode());
        Assert.True(jobTypeArray1 == jobTypeArray2);
    }

    [Fact]
    public void ComplexEquality_AllProperties_MustMatch()
    {
        var enumValues = new[] { "A", "B", "C" };

        // All same - should be equal
        var type1 = new McpToolPropertyType("string", false, enumValues);
        var type2 = new McpToolPropertyType("string", false, new[] { "A", "B", "C" });
        Assert.Equal(type1, type2);

        // Different TypeName - should not be equal
        var type3 = new McpToolPropertyType("number", false, enumValues);
        Assert.NotEqual(type1, type3);

        // Different IsArray - should not be equal
        var type4 = new McpToolPropertyType("string", true, enumValues);
        Assert.NotEqual(type1, type4);

        // Different EnumValues - should not be equal
        var type5 = new McpToolPropertyType("string", false, new[] { "A", "B", "D" });
        Assert.NotEqual(type1, type5);
    }
}
