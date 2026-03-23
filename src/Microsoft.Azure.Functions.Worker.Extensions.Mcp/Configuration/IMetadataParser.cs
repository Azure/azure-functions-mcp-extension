// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Extracts metadata from function parameters.
/// </summary>
internal interface IMetadataParser
{
    /// <summary>
    /// Gets tool metadata JSON from function metadata.
    /// </summary>
    bool TryGetToolMetadata(IFunctionMetadata functionMetadata, [NotNullWhen(true)] out string? metadataJson);

    /// <summary>
    /// Gets resource metadata JSON from function metadata.
    /// </summary>
    bool TryGetResourceMetadata(IFunctionMetadata functionMetadata, [NotNullWhen(true)] out string? metadataJson);
}
