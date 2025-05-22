// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

public class ToolOptions
{
    public void AddProperty(string name, string type, string description)
    {
        Properties.Add(new ToolProperty(name, type, description));
    }

    public required List<ToolProperty> Properties { get; set; } = [];
}