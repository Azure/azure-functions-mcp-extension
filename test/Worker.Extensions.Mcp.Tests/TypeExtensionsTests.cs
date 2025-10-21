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

    [Theory]
    [MemberData(nameof(PocoTestCases))]
    public void IsPoco_ReturnsExpected(Type type, bool expected)
    {
        Assert.Equal(expected, type.IsPoco());
    }

    [Theory]
    [InlineData(typeof(string), "string", false)]
    [InlineData(typeof(int), "integer", false)]
    [InlineData(typeof(int?), "integer", false)]
    [InlineData(typeof(bool), "boolean", false)]
    [InlineData(typeof(bool?), "boolean", false)]
    [InlineData(typeof(double), "number", false)]
    [InlineData(typeof(double?), "number", false)]
    [InlineData(typeof(int[]), "integer", true)]
    [InlineData(typeof(List<string>), "string", true)]
    [InlineData(typeof(IEnumerable<bool>), "boolean", true)]
    [InlineData(typeof(IEnumerable<bool?>), "boolean", true)]
    [InlineData(typeof(CollectionClass), "integer", true)]
    [InlineData(typeof(DateTime), "string", false)]
    [InlineData(typeof(Guid), "string", false)]
    [InlineData(typeof(char), "string", false)]
    [InlineData(typeof(char[]), "string", true)]
    [InlineData(typeof(DateTimeOffset), "string", false)]
    [InlineData(typeof(PocoClass), "object", false)]
    public void MapToToolPropertyType_ReturnsExpectedType(Type type, string expectedType, bool expectedIsArray)
    {
        var expected = new McpToolPropertyType(expectedType, expectedIsArray);

        Assert.Equal(expected, type.MapToToolPropertyType());
    }
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
