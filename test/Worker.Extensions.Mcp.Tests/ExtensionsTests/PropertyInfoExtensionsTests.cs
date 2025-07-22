using System.ComponentModel;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace Worker.Extensions.Mcp.Tests.ExtensionsTests;

public class PropertyInfoExtensionsTests
{
    [Fact]
    public void GetDescription_ReturnsDescription_IfPresent()
    {
        var prop = typeof(TestClass).GetProperty(nameof(TestClass.WithDescription));
        Assert.NotNull(prop);
        Assert.Equal("This is a description.", prop.GetDescription());
    }

    [Fact]
    public void GetDescription_ReturnsNull_IfNotPresent()
    {
        var prop = typeof(TestClass).GetProperty(nameof(TestClass.WithoutDescription));
        Assert.NotNull(prop);
        Assert.Null(prop.GetDescription());
    }

    [Fact]
    public void IsRequired_ReturnsTrue_IfRequiredMemberAttributePresent()
    {
        var prop = typeof(TestClass).GetProperty(nameof(TestClass.RequiredProp));
        Assert.NotNull(prop);
        Assert.True(prop.IsRequired());
    }

    [Fact]
    public void IsRequired_ReturnsFalse_IfRequiredMemberAttributeNotPresent()
    {
        var prop = typeof(TestClass).GetProperty(nameof(TestClass.NotRequiredProp));
        Assert.NotNull(prop);
        Assert.False(prop.IsRequired());
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    class TestClass
    {
        [Description("This is a description.")]
        public string WithDescription { get; set; }

        public string WithoutDescription { get; set; }

        public required string RequiredProp { get; set; }

        public string NotRequiredProp { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
