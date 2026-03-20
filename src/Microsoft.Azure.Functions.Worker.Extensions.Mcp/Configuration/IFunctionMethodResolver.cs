// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Resolves function methods from function metadata using reflection.
/// </summary>
internal interface IFunctionMethodResolver
{
    /// <summary>
    /// Attempts to resolve the method for a function from its metadata.
    /// </summary>
    bool TryResolveMethod(IFunctionMetadata functionMetadata, [NotNullWhen(true)] out MethodInfo? method);
}
