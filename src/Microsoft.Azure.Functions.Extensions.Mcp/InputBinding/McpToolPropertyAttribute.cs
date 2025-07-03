// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.Functions.Extensions.Mcp
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public sealed class McpToolPropertyAttribute(string propertyName, string propertyType, string description, bool required = false) : Attribute, IMcpToolProperty
    {
        public string PropertyName { get; set; } = propertyName;

        public string PropertyType { get; set; } = propertyType;

        public string? Description { get; set; } = description;

        public bool Required { get; set; } = required;
    }
}
