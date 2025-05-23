// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public interface IMcpToolProperty
{
    public string PropertyName { get; set; }

    public string PropertyType { get; set; }

    public string? Description { get; set; }
    
    public bool Required { get; set; }
}
