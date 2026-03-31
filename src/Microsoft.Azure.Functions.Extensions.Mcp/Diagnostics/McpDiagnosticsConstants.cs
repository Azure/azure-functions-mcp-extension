// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

/// <summary>
/// Implementation-specific constants for MCP diagnostics.
/// These are distinct from <see cref="SemanticConventions"/>, which holds OTel spec-defined attribute names.
/// </summary>
internal static class McpDiagnosticsConstants
{
    /// <summary>
    /// The name of the ActivitySource and Meter used for MCP telemetry.
    /// </summary>
    public const string ActivitySourceName = "Azure.Functions.Extensions.Mcp";

    /// <summary>
    /// The version of this instrumentation implementation.
    /// Update when the instrumentation schema (span names, attribute names/types) changes in a breaking way.
    /// </summary>
    public const string ActivitySourceVersion = "2.0.0";
}
