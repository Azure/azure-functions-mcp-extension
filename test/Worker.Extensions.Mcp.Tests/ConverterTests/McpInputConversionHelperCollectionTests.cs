using System.Collections;
using System.Collections.ObjectModel;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

namespace Worker.Extensions.Mcp.Tests.ConverterTests;

public class McpInputConversionHelperCollectionTests
{
    [Fact]
    public void ConvertToCollection_ListOfInt_TargetListInt()
    {
        var source = MakeList(1, 2, 3);

        var result = McpInputConversionHelper.ConvertToCollection(source, typeof(List<int>));

        var list = Assert.IsType<List<int>>(result);
        Assert.Equal([1, 2, 3], list);
    }

    [Fact]
    public void ConvertToCollection_IListOfInt_TargetIListInt()
    {
        var source = MakeList(4, 5);

        var result = McpInputConversionHelper.ConvertToCollection(source, typeof(IList<int>));

        var list = Assert.IsType<List<int>>(result); // Implementation returns List<T> which is assignable to IList<T>
        Assert.Equal([4, 5], list);
    }

    [Fact]
    public void ConvertToCollection_IEnumerableOfInt_TargetIEnumerableInt()
    {
        var source = MakeList(7, 8, 9);

        var result = McpInputConversionHelper.ConvertToCollection(source, typeof(IEnumerable<int>));

        var list = Assert.IsType<List<int>>(result);
        Assert.Equal([7, 8, 9 ], list);
    }

    [Fact]
    public void ConvertToCollection_IEnumerableOfInt_TargetDictionary()
    {
        var source = MakeList(new KeyValuePair<string, string>("test", "test"));

        var result = McpInputConversionHelper.ConvertToCollection(source, typeof(IDictionary<string, string>));

        var list = Assert.IsType<List<KeyValuePair<string, string>>>(result);
        Assert.Equal([new KeyValuePair<string, string>("test", "test")], list);
    }

    [Fact]
    public void ConvertToCollection_NonGenericIEnumerable_TargetEnumerable()
    {
        var source = MakeList(10, 11);

        var result = McpInputConversionHelper.ConvertToCollection(source, typeof(IEnumerable));

        // Non-generic IEnumerable => element type defaults to object, so List<object>
        var list = Assert.IsType<List<object?>>(result);
        Assert.Collection(list,
            v => Assert.Equal(10, v),
            v => Assert.Equal(11, v));
    }

    [Fact]
    public void ConvertToCollection_ArrayInt_TargetIntArray()
    {
        var source = MakeList(1, 2, 3);

        var result = McpInputConversionHelper.ConvertToCollection(source, typeof(int[]));

        var arr = Assert.IsType<int[]>(result);
        Assert.Equal([1, 2, 3], arr);
    }

    [Fact]
    public void ConvertToCollection_ElementConversion_StringNumbersToIntList()
    {
        var source = MakeList("1", "2", "3");

        var result = McpInputConversionHelper.ConvertToCollection(source, typeof(List<int>));

        var list = Assert.IsType<List<int>>(result);
        Assert.Equal([1, 2, 3], list);
    }

    [Fact]
    public void ConvertToCollection_ElementConversion_EnumNamesToEnumList()
    {
        var source = MakeList("Active", "Inactive", "active");

        var result = McpInputConversionHelper.ConvertToCollection(source, typeof(List<StatusEnum>));

        var list = Assert.IsType<List<StatusEnum>>(result);
        Assert.Equal([StatusEnum.Active, StatusEnum.Inactive, StatusEnum.Active], list);
    }

    [Fact]
    public void ConvertToCollection_HashSet_UsesEnumerableConstructor()
    {
        var source = MakeList(2, 2, 3);

        var result = McpInputConversionHelper.ConvertToCollection(source, typeof(HashSet<int>));

        var set = Assert.IsType<HashSet<int>>(result);
        Assert.True(set.SetEquals([2, 3]));
    }

    [Fact]
    public void ConvertToCollection_ReadOnlyCollection_FallsBackToList()
    {
        var source = MakeList(5, 6);

        var result = McpInputConversionHelper.ConvertToCollection(source, typeof(ReadOnlyCollection<int>));

        // No IEnumerable<T> ctor => fallback returns List<T>
        var list = Assert.IsType<List<int>>(result);
        Assert.Equal([5, 6], list);
    }

    [Fact]
    public void ConvertToCollection_CustomCollection_WithEnumerableConstructor()
    {
        var source = MakeList(9, 10);

        var result = McpInputConversionHelper.ConvertToCollection(source, typeof(CustomCollection<int>));

        var custom = Assert.IsType<CustomCollection<int>>(result);
        Assert.Equal([9, 10], custom);
    }

    [Fact]
    public void ConvertToCollection_SourceNull_ReturnsNull()
    {
        var result = McpInputConversionHelper.ConvertToCollection(null!, typeof(List<int>));
        Assert.Null(result);
    }

    private static List<object?> MakeList(params object?[] items) => [.. items];

    private enum StatusEnum
    {
        Inactive = 0,
        Active = 1
    }

    private sealed class CustomCollection<T> : List<T>
    {
        public CustomCollection() { }
        public CustomCollection(IEnumerable<T> items) : base(items) { }
    }
}
