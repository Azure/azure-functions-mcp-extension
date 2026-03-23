// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Represents a tool property binding that can be patched with type information.
/// </summary>
/// <param name="Index">The index of this binding in the raw bindings list.</param>
/// <param name="Binding">The JSON object representing the binding configuration.</param>
internal record ToolPropertyBinding(int Index, JsonObject Binding);
