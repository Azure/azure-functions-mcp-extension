// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

[AttributeUsage(AttributeTargets.Parameter)]
[Binding]
public sealed class McpPromptArgumentAttribute(string argumentName, string? description = null,
                                                bool isRequired = false)
    : Attribute
{
    public string ArgumentName { get; set; } = argumentName;

    public string? Description { get; set; } = description;

    public bool IsRequired { get; set; } = isRequired;
}
