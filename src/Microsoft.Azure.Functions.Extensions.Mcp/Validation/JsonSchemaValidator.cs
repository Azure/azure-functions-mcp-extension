// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Validation;

internal static class JsonSchemaValidator
{
    public static JsonSchemaValidationResult Validate(JsonElement schemaElement, JsonObject instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var schema = JsonSchema.FromText(schemaElement.GetRawText());
        using var instanceDocument = JsonDocument.Parse(instance.ToJsonString());
        var evaluation = schema.Evaluate(instanceDocument.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });

        if (evaluation.IsValid)
        {
            return JsonSchemaValidationResult.Valid;
        }

        var errors = GetErrors(evaluation);
        return new JsonSchemaValidationResult(false, errors.Count > 0 ? errors : ["Validation failed."]);
    }

    private static List<string> GetErrors(EvaluationResults evaluation)
    {
        if (evaluation.Errors is { Count: > 0 } errors)
        {
            return [.. errors.Values];
        }

        if (evaluation.Details is { Count: > 0 } details)
        {
            return [.. details
                .SelectMany(FlattenDetails)
                .Distinct(StringComparer.Ordinal)];
        }

        return [];
    }

    private static IEnumerable<string> FlattenDetails(EvaluationResults detail)
    {
        if (detail.Errors is { Count: > 0 })
        {
            foreach (var message in detail.Errors.Values)
            {
                yield return message;
            }
        }

        if (detail.Details is null)
        {
            yield break;
        }

        foreach (var nestedDetail in detail.Details)
        {
            foreach (var message in FlattenDetails(nestedDetail))
            {
                yield return message;
            }
        }
    }
}