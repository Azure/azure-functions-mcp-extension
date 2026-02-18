// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

/// <summary>
/// Represents a Model Context Protocol (MCP) resource template with RFC 6570-style URI patterns.
/// </summary>
internal interface IMcpResourceTemplate : IMcpResource
{
    /// <summary>
    /// Gets the compiled regex for matching URIs against this template.
    /// </summary>
    Regex TemplateRegex { get; }
}
