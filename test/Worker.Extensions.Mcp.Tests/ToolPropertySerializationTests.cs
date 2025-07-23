using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests;

public class ToolPropertySerializationTests
{
    [Fact]
    public void Serialization_WithRequiredFalse_IncludesRequiredPropertyAsFalse()
    {
        // Arrange
        var property = new ToolProperty("name", "string", "description", false);

        // Act
        var json = JsonSerializer.Serialize(property);

        // Assert
        Assert.Contains("\"propertyName\":\"name\"", json);
        Assert.Contains("\"propertyType\":\"string\"", json);
        Assert.Contains("\"description\":\"description\"", json);
        Assert.Contains("\"required\":false", json);
    }

    [Fact]
    public void Serialization_WithRequiredTrue_IncludesRequiredPropertyAsTrue()
    {
        // Arrange
        var property = new ToolProperty("name", "string", "description", true);

        // Act
        var json = JsonSerializer.Serialize(property);

        // Assert
        Assert.Contains("\"propertyName\":\"name\"", json);
        Assert.Contains("\"propertyType\":\"string\"", json);
        Assert.Contains("\"description\":\"description\"", json);
        Assert.Contains("\"required\":true", json);
    }

    [Fact]
    public void Serialization_DefaultConstructor_HasRequiredFalse()
    {
        // Arrange
        var property = new ToolProperty("name", "string", "description");

        // Act
        var json = JsonSerializer.Serialize(property);

        // Assert
        Assert.Contains("\"required\":false", json);
    }

    [Fact]
    public void Serialization_PropertiesCollection_IncludesRequiredForEachProperty()
    {
        // Arrange
        var properties = new List<ToolProperty>
        {
            new ToolProperty("required-prop", "string", "description", true),
            new ToolProperty("optional-prop", "string", "description", false)
        };

        // Act
        var json = JsonSerializer.Serialize(properties);

        // Assert
        Assert.Contains("\"propertyName\":\"required-prop\"", json);
        Assert.Contains("\"required\":true", json);
        Assert.Contains("\"propertyName\":\"optional-prop\"", json);
        Assert.Contains("\"required\":false", json);
    }
}
