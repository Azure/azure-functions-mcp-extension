// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Extensions.Mcp.Validation;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class JsonSchemaValidatorTests
{
    [Fact]
    public void Validate_ReturnsValidResult_WhenJsonMatchesSchema()
    {
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": { "type": "string" },
                    "age": { "type": "integer" }
                },
                "required": ["name"]
            }
            """).RootElement;

        var instance = new JsonObject
        {
            ["name"] = "Alice",
            ["age"] = 30
        };

        var result = JsonSchemaValidator.Validate(schema, instance);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ReturnsErrors_WhenJsonViolatesSchema()
    {
        var schema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "tags": {
                        "type": "array",
                        "items": { "type": "string" }
                    }
                }
            }
            """).RootElement;

        var instance = new JsonObject
        {
            ["tags"] = new JsonArray(1, 2)
        };

        var result = JsonSchemaValidator.Validate(schema, instance);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }
}