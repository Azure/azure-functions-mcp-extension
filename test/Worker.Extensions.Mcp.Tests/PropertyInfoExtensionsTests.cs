using System.ComponentModel;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;

namespace Worker.Extensions.Mcp.Tests;

public class PropertyInfoExtensionsTests
{
    [Fact]
    public void GetDescription_ReturnsDescription_IfPresent()
    {
        var prop = typeof(BaseWithRequired).GetProperty(nameof(BaseWithRequired.WithDescription));
        Assert.NotNull(prop);
        Assert.Equal("This is a description.", prop.GetDescription());
    }

    [Fact]
    public void GetDescription_ReturnsNull_IfNotPresent()
    {
        var prop = typeof(BaseWithRequired).GetProperty(nameof(BaseWithRequired.WithoutDescription));
        Assert.NotNull(prop);
        Assert.Null(prop.GetDescription());
    }

    [Fact]
    public void IsRequired_DirectlyMarkedRequired_ReturnsTrue()
    {
        var prop = typeof(BaseWithRequired).GetProperty(nameof(BaseWithRequired.BaseRequired))!;
        Assert.True(prop.IsRequired());
    }

    [Fact]
    public void IsRequired_NotMarkedRequired_ReturnsFalse()
    {
        var prop = typeof(BaseWithRequired).GetProperty(nameof(BaseWithRequired.BaseOptional))!;
        Assert.False(prop.IsRequired());
    }

    [Fact]
    public void IsRequired_InheritedProperty_OriginallyRequired_ReturnsTrue()
    {
        var prop = typeof(DerivedWithoutOverride).GetProperty(nameof(BaseWithRequired.BaseRequired))!;
        Assert.True(prop.IsRequired());
    }

    [Fact]
    public void IsRequired_DerivedProperty_NotMarkedRequired_ReturnsFalse()
    {
        var prop = typeof(DerivedWithoutOverride).GetProperty(nameof(DerivedWithoutOverride.DerivedOptional))!;
        Assert.False(prop.IsRequired());
    }

    [Fact]
    public void IsRequired_DerivedProperty_DirectlyMarkedRequired_ReturnsTrue()
    {
        var prop = typeof(DerivedWithNewRequired).GetProperty(nameof(DerivedWithNewRequired.DerivedRequired))!;
        Assert.True(prop.IsRequired());
    }

    [Fact]
    public void IsRequired_UnrelatedTypeProperty_ReturnsFalse()
    {
        var prop = typeof(Unrelated).GetProperty(nameof(Unrelated.UnrelatedProp))!;
        Assert.False(prop.IsRequired());
    }

    class BaseWithRequired
    {
        [Description("This is a description.")]
        public string? WithDescription { get; set; }
        public string? WithoutDescription { get; set; }
        public required string BaseRequired { get; set; }
        public string? BaseOptional { get; set; }
    }

    class DerivedWithoutOverride : BaseWithRequired
    {
        public string? DerivedOptional { get; set; }
    }

    class DerivedWithNewRequired : BaseWithRequired
    {
        public required string DerivedRequired { get; set; }
    }

    class Unrelated
    {
        public string? UnrelatedProp { get; set; }
    }
}
