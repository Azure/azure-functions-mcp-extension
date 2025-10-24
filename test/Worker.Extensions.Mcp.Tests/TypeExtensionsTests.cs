using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;

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
            yield return new object[] { typeof(string), "string", false, null };
            yield return new object[] { typeof(int), "integer", false, null };
            yield return new object[] { typeof(int?), "integer", false, null };
            yield return new object[] { typeof(bool), "boolean", false, null };
            yield return new object[] { typeof(bool?), "boolean", false, null };
            yield return new object[] { typeof(double), "number", false, null };
            yield return new object[] { typeof(double?), "number", false, null };
            yield return new object[] { typeof(int[]), "integer", true, null };
            yield return new object[] { typeof(List<string>), "string", true, null };
            yield return new object[] { typeof(IEnumerable<bool>), "boolean", true, null };
            yield return new object[] { typeof(IEnumerable<bool?>), "boolean", true, null };
            yield return new object[] { typeof(CollectionClass), "integer", true, null };
            yield return new object[] { typeof(DateTime), "string", false, null };
            yield return new object[] { typeof(Guid), "string", false, null };
            yield return new object[] { typeof(char), "string", false, null };
            yield return new object[] { typeof(char[]), "string", true, null };
            yield return new object[] { typeof(DateTimeOffset), "string", false, null };
            yield return new object[] { typeof(PocoClass), "object", false, null };

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
    public void MapToToolPropertyType_ReturnsExpectedType(Type type, string expectedType, bool expectedIsArray, string[]? expectedEnumValues)
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
            Assert.Null(result.EnumValues);
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
