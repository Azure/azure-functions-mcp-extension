// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk;

/// <summary>
/// Internal abstraction for accessing function invocation results.
/// This exists as a workaround for IFunctionBindingsFeature being internal in the Worker SDK.
/// </summary>
internal interface IFunctionResultAccessor
{
    /// <summary>
    /// Gets the current invocation result value.
    /// </summary>
    object? GetResult(FunctionContext context);

    /// <summary>
    /// Sets the invocation result value.
    /// </summary>
    void SetResult(FunctionContext context, object? value);
}
