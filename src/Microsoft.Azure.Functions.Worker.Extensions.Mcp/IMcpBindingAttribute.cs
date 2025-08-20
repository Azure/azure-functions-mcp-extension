// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

public interface IMcpBindingAttribute
{
    /// <summary>
    /// The resolved name of the binding for this attribute.
    /// </summary>
    string BindingName { get; }
}
