// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Contract for metadata attributes that expose a key/value pair.
/// </summary>
internal interface IKeyValueMetadataAttribute
{
    string Key { get; }
    object? Value { get; }
}
