// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.Mcp.Validation;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpToolExtensionsTests
{
    private static IMcpTool CreateTool(params IMcpToolProperty[] properties)
    {
        var mock = new Mock<IMcpTool>();
        var toolInputSchema = new PropertyBasedToolInputSchema(properties);
        mock.SetupGet(t => t.ToolInputSchema).Returns(toolInputSchema);
        mock.SetupGet(t => t.Name).Returns("tool");
        mock.SetupProperty(t => t.Description, "desc");
        mock.Setup(t => t.RunAsync(It.IsAny<RequestContext<CallToolRequestParams>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CallToolResult());
        return mock.Object;
    }

    private static JsonDocument CreateDocumentFromJson(string json)
    {
        return JsonDocument.Parse(json);
    }

    private static IMcpTool CreateToolWithInputSchema(string? inputSchemaJson, params IMcpToolProperty[] properties)
    {
        var mock = new Mock<IMcpTool>();
        
        ToolInputSchema toolInputSchema;
        if (inputSchemaJson != null)
        {
            var schema = CreateDocumentFromJson(inputSchemaJson);
            toolInputSchema = new JsonSchemaToolInputSchema(schema);
        }
        else
        {
            toolInputSchema = new PropertyBasedToolInputSchema(properties);
        }
        
        mock.SetupGet(t => t.ToolInputSchema).Returns(toolInputSchema);
        mock.SetupGet(t => t.Name).Returns("tool");
        mock.SetupProperty(t => t.Description, "desc");
        mock.Setup(t => t.RunAsync(It.IsAny<RequestContext<CallToolRequestParams>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CallToolResult());
        return mock.Object;
    }

    private static IMcpToolProperty CreateProperty(string name, string type, string? description = null, bool required = false, bool isArray = false, string[]? enumValues = null)
    {
        var mock = new Mock<IMcpToolProperty>();
        mock.SetupAllProperties();
        mock.Object.PropertyName = name;
        mock.Object.PropertyType = type;
        mock.Object.Description = description;
        mock.Object.IsRequired = required;
        mock.Object.IsArray = isArray;
        mock.Object.EnumValues = enumValues ?? [];
        return mock.Object;
    }

    [Fact]
    public void GetPropertiesInputSchema_NoProperties_ReturnsEmptySchema()
    {
        var tool = CreateTool();

        var schema = tool.GetPropertiesInputSchema();

        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.Empty(schema.GetProperty("properties").EnumerateObject());
        Assert.Equal(0, schema.GetProperty("required").GetArrayLength());
    }

    [Fact]
    public void GetPropertiesInputSchema_IncludesAllPropertiesAndRequiredArray()
    {
        var prop1 = CreateProperty("first", "string", "First property", required: true);
        var prop2 = CreateProperty("second", "number", null, required: false);
        var tool = CreateTool(prop1, prop2);

        var schema = tool.GetPropertiesInputSchema();

        Assert.Equal("object", schema.GetProperty("type").GetString());
        var properties = schema.GetProperty("properties");

        var first = properties.GetProperty("first");
        Assert.Equal("string", first.GetProperty("type").GetString());
        Assert.Equal("First property", first.GetProperty("description").GetString());

        var second = properties.GetProperty("second");
        Assert.Equal("number", second.GetProperty("type").GetString());
        // Null descriptions become empty string
        Assert.Equal(string.Empty, second.GetProperty("description").GetString());

        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(required);
        Assert.Contains("first", required);
    }

    [Fact]
    public void GetPropertiesInputSchema_ArrayProperty_SerializesWithArrayTypeAndItems()
    {
        var arrayProp = CreateProperty("tags", "string", "Tags list", required: true, isArray: true);
        var scalarProp = CreateProperty("count", "number", null, required: false, isArray: false);
        var tool = CreateTool(arrayProp, scalarProp);

        var schema = tool.GetPropertiesInputSchema();

        var properties = schema.GetProperty("properties");
        var tags = properties.GetProperty("tags");
        Assert.Equal("array", tags.GetProperty("type").GetString());

        var items = tags.GetProperty("items");
        Assert.Equal("string", items.GetProperty("type").GetString());
        Assert.Equal("Tags list", tags.GetProperty("description").GetString());

        var count = properties.GetProperty("count");
        Assert.Equal("number", count.GetProperty("type").GetString());
        Assert.False(count.TryGetProperty("items", out var _));

        Assert.Equal(string.Empty, count.GetProperty("description").GetString());

        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(required);
        Assert.Contains("tags", required);
    }

    [Fact]
    public void GetPropertiesInputSchema_UsesInputSchema_WhenPopulated()
    {
        // Arrange - Create an input schema with specific properties
        var inputSchemaJson = """
            {
                "type": "object",
                "properties": {
                    "schemaProperty": {
                        "type": "string",
                        "description": "Property from schema"
                    },
                    "anotherSchemaProperty": {
                        "type": "number",
                        "description": "Another property from schema"
                    }
                },
                "required": ["schemaProperty"]
            }
            """;

        // Create tool properties that should be ignored when input schema is present
        var toolProperty = CreateProperty("toolProperty", "boolean", "Property from tool", required: true);
        var tool = CreateToolWithInputSchema(inputSchemaJson, toolProperty);

        // Act
        var schema = tool.GetPropertiesInputSchema();

        // Assert - Should use input schema, not tool properties
        Assert.Equal("object", schema.GetProperty("type").GetString());
        
        var properties = schema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("schemaProperty", out var schemaProp));
        Assert.Equal("string", schemaProp.GetProperty("type").GetString());
        Assert.Equal("Property from schema", schemaProp.GetProperty("description").GetString());

        Assert.True(properties.TryGetProperty("anotherSchemaProperty", out var anotherSchemaProp));
        Assert.Equal("number", anotherSchemaProp.GetProperty("type").GetString());
        Assert.Equal("Another property from schema", anotherSchemaProp.GetProperty("description").GetString());

        // Should NOT contain tool properties
        Assert.False(properties.TryGetProperty("toolProperty", out var _));

        // Required array should come from input schema
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(required);
        Assert.Contains("schemaProperty", required);
        Assert.DoesNotContain("toolProperty", required);
    }

    [Fact]
    public void GetPropertiesInputSchema_UsesInputSchema_WithArrayProperties()
    {
        // Arrange - Create an input schema with array properties
        var inputSchemaJson = """
            {
                "type": "object",
                "properties": {
                    "arrayProp": {
                        "type": "array",
                        "description": "Array property from schema",
                        "items": {
                            "type": "string"
                        }
                    },
                    "scalarProp": {
                        "type": "boolean",
                        "description": "Scalar property from schema"
                    }
                },
                "required": ["arrayProp", "scalarProp"]
            }
            """;

        var tool = CreateToolWithInputSchema(inputSchemaJson);

        // Act
        var schema = tool.GetPropertiesInputSchema();

        // Assert
        var properties = schema.GetProperty("properties");
        
        Assert.True(properties.TryGetProperty("arrayProp", out var arrayProp));
        Assert.Equal("array", arrayProp.GetProperty("type").GetString());
        Assert.Equal("Array property from schema", arrayProp.GetProperty("description").GetString());
        Assert.True(arrayProp.TryGetProperty("items", out var items));
        Assert.Equal("string", items.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("scalarProp", out var scalarProp));
        Assert.Equal("boolean", scalarProp.GetProperty("type").GetString());
        Assert.Equal("Scalar property from schema", scalarProp.GetProperty("description").GetString());

        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(2, required.Length);
        Assert.Contains("arrayProp", required);
        Assert.Contains("scalarProp", required);
    }

    [Fact]
    public void GetPropertiesInputSchema_UsesInputSchema_WithEmptyRequired()
    {
        // Arrange - Create an input schema with no required properties
        var inputSchemaJson = """
            {
                "type": "object",
                "properties": {
                    "optionalProp": {
                        "type": "string",
                        "description": "Optional property"
                    }
                },
                "required": []
            }
            """;

        // Create a tool property that would be required if input schema wasn't present
        var requiredToolProperty = CreateProperty("requiredToolProp", "number", "Required tool property", required: true);
        var tool = CreateToolWithInputSchema(inputSchemaJson, requiredToolProperty);

        // Act
        var schema = tool.GetPropertiesInputSchema();

        // Assert - Should use input schema with empty required array
        var properties = schema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("optionalProp", out var _));
        Assert.False(properties.TryGetProperty("requiredToolProp", out var _));

        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Empty(required);
    }

    [Fact]
    public void GetPropertiesInputSchema_FallsBackToProperties_WhenInputSchemaIsDefault()
    {
        // Arrange - Create tool with default input schema but with tool properties
        var toolProperty = CreateProperty("toolProp", "string", "Tool property", required: true);
        var tool = CreateToolWithInputSchema(null, toolProperty);

        // Act
        var schema = tool.GetPropertiesInputSchema();

        // Assert - Should fall back to tool properties
        var properties = schema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("toolProp", out var prop));
        Assert.Equal("string", prop.GetProperty("type").GetString());
        Assert.Equal("Tool property", prop.GetProperty("description").GetString());

        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(required);
        Assert.Contains("toolProp", required);
    }

    [Fact]
    public void GetPropertiesInputSchema_EnumProperty_SerializesAsStringWithEnumConstraints()
    {
        // Now enums use "string" as PropertyType but have enumValues populated
        var enumProp = CreateProperty("status", "string", "Status value", required: true, 
                                    enumValues: new[] { "Active", "Inactive", "Pending" });
        var tool = CreateTool(enumProp);

        var schema = tool.GetPropertiesInputSchema();

        var properties = schema.GetProperty("properties");
        var status = properties.GetProperty("status");
        
        // Should be string type in JSON schema
        Assert.Equal("string", status.GetProperty("type").GetString());
        Assert.Equal("Status value", status.GetProperty("description").GetString());
        
        // Should have enum constraint
        Assert.True(status.TryGetProperty("enum", out var enumProperty));
        var enumValues = enumProperty.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(new[] { "Active", "Inactive", "Pending" }, enumValues);
    }

    [Fact]
    public void GetPropertiesInputSchema_EnumArrayProperty_SerializesAsArrayWithEnumConstraints()
    {
        // Now enums use "string" as PropertyType but have enumValues populated
        var enumArrayProp = CreateProperty("statuses", "string", "Status values", required: false, 
                                         isArray: true, enumValues: new[] { "Active", "Inactive", "Pending" });
        var tool = CreateTool(enumArrayProp);

        var schema = tool.GetPropertiesInputSchema();

        var properties = schema.GetProperty("properties");
        var statuses = properties.GetProperty("statuses");
        
        // Should be array type
        Assert.Equal("array", statuses.GetProperty("type").GetString());
        Assert.Equal("Status values", statuses.GetProperty("description").GetString());
        
        // Items should be string with enum constraint
        var items = statuses.GetProperty("items");
        Assert.Equal("string", items.GetProperty("type").GetString());
        
        Assert.True(items.TryGetProperty("enum", out var enumProperty));
        var enumValues = enumProperty.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(new[] { "Active", "Inactive", "Pending" }, enumValues);
    }

    [Fact]
    public void GetPropertiesInputSchema_JobTypeEnum_GeneratesCorrectSchema()
    {
        // Validate the original user's JobType enum scenario - now using "string" PropertyType
        var jobTypeEnum = CreateProperty("job", "string", "The job of the person.", required: true,
                                       enumValues: new[] { "FullTime", "PartTime", "Contract", "Internship", "Temporary", "Freelance", "Unemployed" });
        var tool = CreateTool(jobTypeEnum);

        var schema = tool.GetPropertiesInputSchema();

        var properties = schema.GetProperty("properties");
        var job = properties.GetProperty("job");
        
        // Should be string type with enum constraints
        Assert.Equal("string", job.GetProperty("type").GetString());
        Assert.Equal("The job of the person.", job.GetProperty("description").GetString());
        
        // Should have all JobType enum values
        Assert.True(job.TryGetProperty("enum", out var enumValues));
        var actualValues = enumValues.EnumerateArray().Select(e => e.GetString()).ToArray();
        var expectedValues = new[] { "FullTime", "PartTime", "Contract", "Internship", "Temporary", "Freelance", "Unemployed" };
        Assert.Equal(expectedValues, actualValues);
        
        // Should be required
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(required);
        Assert.Contains("job", required);
    }

    [Fact]
    public void GetPropertiesInputSchema_JobTypeEnumArray_GeneratesCorrectSchema()
    {
        // Validate IEnumerable<JobType> scenario from original user request - now using "string" PropertyType
        var jobTypesArray = CreateProperty("jobs", "string", "The job types of the person.", required: false, isArray: true,
                                         enumValues: new[] { "FullTime", "PartTime", "Contract", "Internship", "Temporary", "Freelance", "Unemployed" });
        var tool = CreateTool(jobTypesArray);

        var schema = tool.GetPropertiesInputSchema();

        var properties = schema.GetProperty("properties");
        var jobs = properties.GetProperty("jobs");
        
        // Should be array type
        Assert.Equal("array", jobs.GetProperty("type").GetString());
        Assert.Equal("The job types of the person.", jobs.GetProperty("description").GetString());
        
        // Items should be string with JobType enum constraints
        var items = jobs.GetProperty("items");
        Assert.Equal("string", items.GetProperty("type").GetString());
        
        Assert.True(items.TryGetProperty("enum", out var enumValues));
        var actualValues = enumValues.EnumerateArray().Select(e => e.GetString()).ToArray();
        var expectedValues = new[] { "FullTime", "PartTime", "Contract", "Internship", "Temporary", "Freelance", "Unemployed" };
        Assert.Equal(expectedValues, actualValues);
        
        // Should not be required (empty required array)
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Empty(required);
    }

    [Fact]
    public void GetPropertiesInputSchema_MixedPropertiesWithEnum_GeneratesCorrectSchema()
    {
        // This represents the complete HappyFunction scenario with both enum types and regular properties
        var jobProperty = CreateProperty("job", "string", "The job of the person.", required: true,
                                       enumValues: new[] { "FullTime", "PartTime", "Contract", "Internship", "Temporary", "Freelance", "Unemployed" });
        var jobsProperty = CreateProperty("jobs", "string", "The job types of the person.", required: false, isArray: true,
                                        enumValues: new[] { "FullTime", "PartTime", "Contract", "Internship", "Temporary", "Freelance", "Unemployed" });
        var nameProperty = CreateProperty("name", "string", "The name of the person to greet.", required: true);
        var ageProperty = CreateProperty("age", "integer", "The age of the person.", required: false);
        
        var tool = CreateTool(jobProperty, jobsProperty, nameProperty, ageProperty);

        var schema = tool.GetPropertiesInputSchema();

        Assert.Equal("object", schema.GetProperty("type").GetString());
        var properties = schema.GetProperty("properties");

        // Validate single enum property
        var job = properties.GetProperty("job");
        Assert.Equal("string", job.GetProperty("type").GetString());
        Assert.True(job.TryGetProperty("enum", out var jobEnumValues));
        Assert.Contains("FullTime", jobEnumValues.EnumerateArray().Select(e => e.GetString()));
        Assert.Contains("Unemployed", jobEnumValues.EnumerateArray().Select(e => e.GetString()));

        // Validate enum array property
        var jobs = properties.GetProperty("jobs");
        Assert.Equal("array", jobs.GetProperty("type").GetString());
        var jobsItems = jobs.GetProperty("items");
        Assert.Equal("string", jobsItems.GetProperty("type").GetString());
        Assert.True(jobsItems.TryGetProperty("enum", out var jobsEnumValues));
        Assert.Contains("PartTime", jobsEnumValues.EnumerateArray().Select(e => e.GetString()));

        // Validate regular properties don't have enum constraints
        var name = properties.GetProperty("name");
        Assert.Equal("string", name.GetProperty("type").GetString());
        Assert.False(name.TryGetProperty("enum", out var _));

        var age = properties.GetProperty("age");
        Assert.Equal("integer", age.GetProperty("type").GetString());
        Assert.False(age.TryGetProperty("enum", out var _));

        // Validate required properties
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(2, required.Length);
        Assert.Contains("job", required);
        Assert.Contains("name", required);
    }

    [Fact]
    public void GetPropertiesInputSchema_EmptyEnumValues_DoesNotAddEnumConstraints()
    {
        var enumProperty = CreateProperty("emptyEnum", "string", "Empty enum property", enumValues: []);
        var tool = CreateTool(enumProperty);

        var schema = tool.GetPropertiesInputSchema();

        var properties = schema.GetProperty("properties");
        var emptyEnum = properties.GetProperty("emptyEnum");
        Assert.Equal("string", emptyEnum.GetProperty("type").GetString());
        
        // Should not have enum constraints when enum values are empty
        Assert.False(emptyEnum.TryGetProperty("enum", out var _));
    }

    [Fact]
    public void GetPropertiesInputSchema_NullEnumValues_DoesNotAddEnumConstraints()
    {
        var enumProperty = CreateProperty("nullEnum", "string", "Null enum property", enumValues: null);
        var tool = CreateTool(enumProperty);

        var schema = tool.GetPropertiesInputSchema();

        var properties = schema.GetProperty("properties");
        var nullEnum = properties.GetProperty("nullEnum");
        Assert.Equal("string", nullEnum.GetProperty("type").GetString());
        
        // Should not have enum constraints when enum values are null
        Assert.False(nullEnum.TryGetProperty("enum", out var _));
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]  
    [InlineData("High")]
    [InlineData("Critical")]
    public void ValidateGeneratedSchema_AllowsValidEnumValues(string validValue)
    {
        var enumValues = new[] { "Low", "Medium", "High", "Critical" };
        var enumProperty = CreateProperty("priority", "string", "Priority level", enumValues: enumValues);
        var tool = CreateTool(enumProperty);
        var schema = tool.GetPropertiesInputSchema();

        // Extract enum constraints from schema
        var properties = schema.GetProperty("properties");
        var priority = properties.GetProperty("priority");
        var enumConstraints = priority.GetProperty("enum");
        var allowedValues = enumConstraints.EnumerateArray().Select(e => e.GetString()).ToArray();

        // Verify the valid value is in the allowed list
        Assert.Contains(validValue, allowedValues);
    }

    [Fact]
    public void GetPropertiesInputSchema_ExpectedJsonFormat_MatchesSpecification()
    {
        // This test validates the exact format expected for enum schema generation
        var jobTypeEnum = CreateProperty("jobType", "string", "The job type of the person.", required: true,
                                       enumValues: new[] { "FullTime", "PartTime", "Contract" });
        var tool = CreateTool(jobTypeEnum);

        var schema = tool.GetPropertiesInputSchema();

        // The generated schema should match this structure:
        // {
        //   "type": "object",
        //   "properties": {
        //     "jobType": {
        //       "type": "string",
        //       "enum": ["FullTime", "PartTime", "Contract"],
        //       "description": "The job type of the person."
        //     }
        //   },
        //   "required": ["jobType"]
        // }

        Assert.Equal("object", schema.GetProperty("type").GetString());
        
        var properties = schema.GetProperty("properties");
        var jobType = properties.GetProperty("jobType");
        
        Assert.Equal("string", jobType.GetProperty("type").GetString());
        Assert.True(jobType.TryGetProperty("enum", out var enumProperty));
        
        var actualEnumValues = enumProperty.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(new[] { "FullTime", "PartTime", "Contract" }, actualEnumValues);
        
        Assert.Equal("The job type of the person.", jobType.GetProperty("description").GetString());
        
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Single(required);
        Assert.Contains("jobType", required);
    }

    protected virtual void Dispose(bool disposing)
    {
        // Cleanup for any disposable resources if needed
    }
}
