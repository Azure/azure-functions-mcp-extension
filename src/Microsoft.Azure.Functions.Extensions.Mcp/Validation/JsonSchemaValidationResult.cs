// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp.Validation;

internal sealed record JsonSchemaValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static JsonSchemaValidationResult Valid { get; } = new(true, []);

    public string ErrorMessage => Errors.Count > 0
        ? string.Join(" ", Errors)
        : "Validation failed.";
}