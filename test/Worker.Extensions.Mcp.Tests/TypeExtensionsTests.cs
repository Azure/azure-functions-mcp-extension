using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;
using System.Runtime.Serialization;

namespace Worker.Extensions.Mcp.Tests;

public class TypeExtensionsTests
{
    public static IEnumerable<object[]> PocoTestCases =>
    [
        [typeof(PocoClass), true],
        [typeof(EmptyClass), true],
        [typeof(ClassWithFieldsOnly), true],
        [typeof(NestedPocoClass), true],
        [typeof(PocoWithPrivateFields), true],
        [typeof(GenericClass<string>), true],
        [typeof(ClassWithStaticCtor), true],
        [typeof(ClassWithInitOnly), true],

        [typeof(string), false],
        [typeof(AbstractClass), false],
        [typeof(ITest), false],
        [typeof(CollectionClass), false],
        [typeof(NoDefaultCtor), false],
        [typeof(GenericClass<>), false],
        [typeof(StructExample), false],
        [typeof(RecordExample), false],
        [typeof(CollectionDerived), false]
    ];

    public static IEnumerable<object[]> MapToToolPropertyTypeTestCases
    {
        get
        {
            // Basic non-enum types - no enum values expected
            yield return new object[] { typeof(string), "string", false };
            yield return new object[] { typeof(int), "integer", false };
            yield return new object[] { typeof(int?), "integer", false };
            yield return new object[] { typeof(bool), "boolean", false };
            yield return new object[] { typeof(bool?), "boolean", false };
            yield return new object[] { typeof(double), "number", false };
            yield return new object[] { typeof(double?), "number", false };
            yield return new object[] { typeof(int[]), "integer", true };
            yield return new object[] { typeof(List<string>), "string", true };
            yield return new object[] { typeof(IEnumerable<bool>), "boolean", true };
            yield return new object[] { typeof(IEnumerable<bool?>), "boolean", true };
            yield return new object[] { typeof(CollectionClass), "integer", true };
            yield return new object[] { typeof(DateTime), "string", false };
            yield return new object[] { typeof(Guid), "string", false };
            yield return new object[] { typeof(char), "string", false };
            yield return new object[] { typeof(char[]), "string", true };
            yield return new object[] { typeof(DateTimeOffset), "string", false };
            yield return new object[] { typeof(PocoClass), "object", false };

            // TestEnum cases - should return "string" with TestEnum values
            var testEnumValues = new[] { "Value1", "Value2", "Value3" };
            yield return new object[] { typeof(TestEnum), "string", false, testEnumValues };
            yield return new object[] { typeof(TestEnum[]), "string", true, testEnumValues };
            yield return new object[] { typeof(IEnumerable<TestEnum>), "string", true, testEnumValues };
            yield return new object[] { typeof(List<TestEnum>), "string", true, testEnumValues };
            yield return new object[] { typeof(ICollection<TestEnum>), "string", true, testEnumValues };
            yield return new object[] { typeof(IList<TestEnum>), "string", true, testEnumValues };
            yield return new object[] { typeof(TestEnum?), "string", false, testEnumValues };

            // JobType cases - should return "string" with JobType values
            var jobTypeValues = new[] { "FullTime", "PartTime", "Contract", "Internship", "Temporary", "Freelance", "Unemployed" };
            yield return new object[] { typeof(JobType), "string", false, jobTypeValues };
            yield return new object[] { typeof(JobType[]), "string", true, jobTypeValues };
            yield return new object[] { typeof(IEnumerable<JobType>), "string", true, jobTypeValues };
            yield return new object[] { typeof(List<JobType>), "string", true, jobTypeValues };
            yield return new object[] { typeof(ICollection<JobType>), "string", true, jobTypeValues };
            yield return new object[] { typeof(IList<JobType>), "string", true, jobTypeValues };
            yield return new object[] { typeof(JobType?), "string", false, jobTypeValues };

            // CustomizedStatusEnum cases - should return customized values, not raw enum names
            var customizedStatusValues = new[] { "active", "inactive", "suspended", "archived" };
            yield return new object[] { typeof(CustomizedStatusEnum), "string", false, customizedStatusValues };
            yield return new object[] { typeof(CustomizedStatusEnum[]), "string", true, customizedStatusValues };
            yield return new object[] { typeof(List<CustomizedStatusEnum>), "string", true, customizedStatusValues };
            yield return new object[] { typeof(CustomizedStatusEnum?), "string", false, customizedStatusValues };

            // MixedCustomizationEnum cases - should return mix of customized and default values
            var mixedCustomizationValues = new[] { "custom_high", "Medium", "custom_low" };
            yield return new object[] { typeof(MixedCustomizationEnum), "string", false, mixedCustomizationValues };
            yield return new object[] { typeof(MixedCustomizationEnum[]), "string", true, mixedCustomizationValues };
        }
    }

    [Theory]
    [MemberData(nameof(PocoTestCases))]
    public void IsPoco_ReturnsExpected(Type type, bool expected)
    {
        Assert.Equal(expected, type.IsPoco());
    }

    [Theory]
    [MemberData(nameof(MapToToolPropertyTypeTestCases))]
    public void MapToToolPropertyType_ReturnsExpectedType(Type type, string expectedType, bool expectedIsArray, string[] expectedEnumValues = default!)
    {
        // Act
        var result = type.MapToToolPropertyType();
        
        // Assert basic type properties
        Assert.Equal(expectedType, result.TypeName);
        Assert.Equal(expectedIsArray, result.IsArray);
        
        // Assert enum values
        if (expectedEnumValues != null)
        {
            // Enum types should have enum values
            Assert.NotNull(result.EnumValues);
            Assert.Equal(expectedEnumValues, result.EnumValues);
        }
        else
        {
            // Non-enum types should not have enum values
            Assert.Equal(Array.Empty<string>(), result.EnumValues);
        }
    }

    [Theory]
    [InlineData(nameof(TestParameters.SingleJob), "string", false)]
    [InlineData(nameof(TestParameters.MultipleJobs), "string", true)]
    [InlineData(nameof(TestParameters.NullableJob), "string", false)]
    [InlineData(nameof(TestParameters.JobList), "string", true)]
    [InlineData(nameof(TestParameters.JobArray), "string", true)]
    public void PocoProperties_MapToCorrectEnumTypes(string propertyName, string expectedTypeName, bool expectedIsArray)
    {
        // Arrange
        var property = typeof(TestParameters).GetProperty(propertyName);
        Assert.NotNull(property);

        // Act
        var result = property.PropertyType.MapToToolPropertyType();

        // Assert
        Assert.Equal(expectedTypeName, result.TypeName);
        Assert.Equal(expectedIsArray, result.IsArray);
        Assert.NotNull(result.EnumValues);

        var expectedValues = new[] { "FullTime", "PartTime", "Contract", "Internship", "Temporary", "Freelance", "Unemployed" };
        Assert.Equal(expectedValues, result.EnumValues);
    }

    [Fact]
    public void MapToToolPropertyType_EnumWithoutCustomization_UsesEnumNames()
    {
        // Act
        var result = typeof(TestEnum).MapToToolPropertyType();

        // Assert
        Assert.Equal("string", result.TypeName);
        Assert.False(result.IsArray);
        Assert.True(result.IsEnum);
        Assert.NotNull(result.EnumValues);
        Assert.Equal(new[] { "Value1", "Value2", "Value3" }, result.EnumValues);
    }

    [Fact]
    public void MapToToolPropertyType_EnumWithFullCustomization_UsesCustomNames()
    {
        // Act
        var result = typeof(CustomizedStatusEnum).MapToToolPropertyType();

        // Assert
        Assert.Equal("string", result.TypeName);
        Assert.False(result.IsArray);
        Assert.True(result.IsEnum);
        Assert.NotNull(result.EnumValues);
        
        // Should use the EnumMember values, not the enum names
        var expectedCustomValues = new[] { "active", "inactive", "suspended", "archived" };
        Assert.Equal(expectedCustomValues, result.EnumValues);
        
        // Verify it's NOT using the raw enum names
        var rawEnumNames = new[] { "Active", "Inactive", "Suspended", "Archived" };
        Assert.NotEqual(rawEnumNames, result.EnumValues);
    }

    [Fact]
    public void MapToToolPropertyType_EnumWithMixedCustomization_UsesMixOfCustomAndDefaultNames()
    {
        // Act
        var result = typeof(MixedCustomizationEnum).MapToToolPropertyType();

        // Assert
        Assert.Equal("string", result.TypeName);
        Assert.False(result.IsArray);
        Assert.True(result.IsEnum);
        Assert.NotNull(result.EnumValues);
        
        // Should use custom values where available, default names otherwise
        var expectedMixedValues = new[] { "custom_high", "Medium", "custom_low" };
        Assert.Equal(expectedMixedValues, result.EnumValues);
    }

    [Fact]
    public void MapToToolPropertyType_CustomizedEnumArray_PreservesCustomization()
    {
        // Act
        var result = typeof(CustomizedStatusEnum[]).MapToToolPropertyType();

        // Assert
        Assert.Equal("string", result.TypeName);
        Assert.True(result.IsArray);
        Assert.True(result.IsEnum);
        Assert.NotNull(result.EnumValues);
        
        var expectedCustomValues = new[] { "active", "inactive", "suspended", "archived" };
        Assert.Equal(expectedCustomValues, result.EnumValues);
    }

    [Fact]
    public void MapToToolPropertyType_NullableCustomizedEnum_PreservesCustomization()
    {
        // Act
        var result = typeof(CustomizedStatusEnum?).MapToToolPropertyType();

        // Assert
        Assert.Equal("string", result.TypeName);
        Assert.False(result.IsArray);
        Assert.True(result.IsEnum);
        Assert.NotNull(result.EnumValues);
        
        var expectedCustomValues = new[] { "active", "inactive", "suspended", "archived" };
        Assert.Equal(expectedCustomValues, result.EnumValues);
    }

    [Fact]
    public void MapToToolPropertyType_GenericCollectionOfCustomizedEnum_PreservesCustomization()
    {
        // Act
        var listResult = typeof(List<CustomizedStatusEnum>).MapToToolPropertyType();
        var enumerableResult = typeof(IEnumerable<CustomizedStatusEnum>).MapToToolPropertyType();

        // Assert List<CustomizedStatusEnum>
        Assert.Equal("string", listResult.TypeName);
        Assert.True(listResult.IsArray);
        Assert.True(listResult.IsEnum);
        var expectedCustomValues = new[] { "active", "inactive", "suspended", "archived" };
        Assert.Equal(expectedCustomValues, listResult.EnumValues);

        // Assert IEnumerable<CustomizedStatusEnum>
        Assert.Equal("string", enumerableResult.TypeName);
        Assert.True(enumerableResult.IsArray);
        Assert.True(enumerableResult.IsEnum);
        Assert.Equal(expectedCustomValues, enumerableResult.EnumValues);
    }

    [Fact]
    public void MapToToolPropertyType_HttpMethodEnum_UsesStandardHttpMethodNames()
    {
        // Act
        var result = typeof(HttpMethodEnum).MapToToolPropertyType();

        // Assert
        Assert.Equal("string", result.TypeName);
        Assert.False(result.IsArray);
        Assert.True(result.IsEnum);
        Assert.NotNull(result.EnumValues);
        
        // Should use HTTP method names as customized
        var expectedHttpMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };
        Assert.Equal(expectedHttpMethods, result.EnumValues);
    }

    [Fact]
    public void MapToToolPropertyType_PriorityEnum_UsesNumericCustomization()
    {
        // Act
        var result = typeof(PriorityEnum).MapToToolPropertyType();

        // Assert
        Assert.Equal("string", result.TypeName);
        Assert.False(result.IsArray);
        Assert.True(result.IsEnum);
        Assert.NotNull(result.EnumValues);
        
        // Should use numeric priority values
        var expectedPriorities = new[] { "1", "2", "3", "4", "5" };
        Assert.Equal(expectedPriorities, result.EnumValues);
    }

    [Fact]
    public void MapToToolPropertyType_EmptyEnumMemberValue_KeepsEmptyCustomizedValue()
    {
        // Act
        var result = typeof(EnumWithEmptyCustomization).MapToToolPropertyType();

        // Assert
        Assert.Equal("string", result.TypeName);
        Assert.False(result.IsArray);
        Assert.True(result.IsEnum);
        Assert.NotNull(result.EnumValues);

        // Should keep empty customized value
        var expectedValues = new[] { "ValidCustom", "" };
        Assert.Equal(expectedValues, result.EnumValues);
    }

    /// <summary>
    /// Test class to simulate function parameter classes with customized enum properties
    /// </summary>
    public class CustomizedTestParameters
    {
        public CustomizedStatusEnum Status { get; set; }
        public IEnumerable<CustomizedStatusEnum> Statuses { get; set; } = [];
        public CustomizedStatusEnum? OptionalStatus { get; set; }
        public List<MixedCustomizationEnum> Priorities { get; set; } = [];
        public HttpMethodEnum[] Methods { get; set; } = [];
    }

    [Theory]
    [InlineData(nameof(CustomizedTestParameters.Status), "string", false)]
    [InlineData(nameof(CustomizedTestParameters.Statuses), "string", true)]
    [InlineData(nameof(CustomizedTestParameters.OptionalStatus), "string", false)]
    [InlineData(nameof(CustomizedTestParameters.Priorities), "string", true)]
    [InlineData(nameof(CustomizedTestParameters.Methods), "string", true)]
    public void PocoProperties_MapToCorrectCustomizedEnumTypes(string propertyName, string expectedTypeName, bool expectedIsArray)
    {
        // Arrange
        var property = typeof(CustomizedTestParameters).GetProperty(propertyName);
        Assert.NotNull(property);

        // Act
        var result = property.PropertyType.MapToToolPropertyType();

        // Assert
        Assert.Equal(expectedTypeName, result.TypeName);
        Assert.Equal(expectedIsArray, result.IsArray);
        Assert.NotNull(result.EnumValues);
        Assert.True(result.IsEnum);
        
        // Verify each property has the expected customized values
        switch (propertyName)
        {
            case nameof(CustomizedTestParameters.Status):
            case nameof(CustomizedTestParameters.Statuses):
            case nameof(CustomizedTestParameters.OptionalStatus):
                Assert.Equal(new[] { "active", "inactive", "suspended", "archived" }, result.EnumValues);
                break;
            case nameof(CustomizedTestParameters.Priorities):
                Assert.Equal(new[] { "custom_high", "Medium", "custom_low" }, result.EnumValues);
                break;
            case nameof(CustomizedTestParameters.Methods):
                Assert.Equal(new[] { "GET", "POST", "PUT", "DELETE", "PATCH" }, result.EnumValues);
                break;
        }
    }
}

/// <summary>
/// Test enum representing job types from the original user scenario
/// </summary>
public enum JobType
{
    FullTime,
    PartTime,
    Contract,
    Internship,
    Temporary,
    Freelance,
    Unemployed
}

public enum TestEnum
{
    Value1,
    Value2,
    Value3
}

/// <summary>
/// Test enum with full EnumMember customization - all values have custom names
/// </summary>
public enum CustomizedStatusEnum
{
    [EnumMember(Value = "active")]
    Active,
    
    [EnumMember(Value = "inactive")]
    Inactive,
    
    [EnumMember(Value = "suspended")]
    Suspended,
    
    [EnumMember(Value = "archived")]
    Archived
}

/// <summary>
/// Test enum with mixed customization - some values customized, some use default names
/// </summary>
public enum MixedCustomizationEnum
{
    [EnumMember(Value = "custom_high")]
    High,
    
    Medium, // No EnumMember attribute - should use "Medium"
    
    [EnumMember(Value = "custom_low")]
    Low
}

/// <summary>
/// Test enum representing HTTP methods with appropriate customization
/// </summary>
public enum HttpMethodEnum
{
    [EnumMember(Value = "GET")]
    Get,
    
    [EnumMember(Value = "POST")]
    Post,
    
    [EnumMember(Value = "PUT")]
    Put,
    
    [EnumMember(Value = "DELETE")]
    Delete,
    
    [EnumMember(Value = "PATCH")]
    Patch
}

/// <summary>
/// Test enum with numeric customization for priority levels
/// </summary>
public enum PriorityEnum
{
    [EnumMember(Value = "1")]
    Lowest,
    
    [EnumMember(Value = "2")]
    Low,
    
    [EnumMember(Value = "3")]
    Normal,
    
    [EnumMember(Value = "4")]
    High,
    
    [EnumMember(Value = "5")]
    Critical
}

/// <summary>
/// Test enum to verify behavior when EnumMember.Value is empty or null
/// </summary>
public enum EnumWithEmptyCustomization
{
    [EnumMember(Value = "ValidCustom")]
    FirstValue,
    
    [EnumMember(Value = "")] // Empty value should fall back to enum name
    EmptyCustomization
}

/// <summary>
/// Test class to simulate function parameter classes with enum properties
/// </summary>
public class TestParameters
{
    public JobType SingleJob { get; set; }
    public IEnumerable<JobType> MultipleJobs { get; set; } = [];
    public JobType? NullableJob { get; set; }
    public List<JobType> JobList { get; set; } = [];
    public JobType[] JobArray { get; set; } = [];
}

#pragma warning disable 0649, 0169, 8618
// Valid POCO classes
class PocoClass
{
    public int X { get; set; }
}

class ClassWithFieldsOnly
{
    public int Field1;
    public string Field2;
}

class NestedPocoClass
{
    public PocoClass Child { get; set; }
}

class PocoWithPrivateFields
{
    private int _x;
    public string Name { get; set; }
}

class GenericClass<T>
{
    public T Value { get; set; }
}

class ClassWithStaticCtor
{
    static ClassWithStaticCtor() { }
    public int Id { get; set; }
}

class ClassWithInitOnly
{
    public int Id { get; init; }
}

class EmptyClass { }

// Invalid POCO classes
class NoDefaultCtor
{
    public NoDefaultCtor(int x) { }
}

struct StructExample
{
    public int X;
}

abstract class AbstractClass { }

interface ITest { }

class CollectionClass : List<int> { }

record RecordExample(int Id);

class CollectionDerived : Dictionary<string, int> { }
#pragma warning restore 0649, 0169, 8618
