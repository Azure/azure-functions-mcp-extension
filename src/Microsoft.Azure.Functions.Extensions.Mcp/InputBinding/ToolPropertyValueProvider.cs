// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public class ToolPropertyValueProvider(object? value, Type? type) : IValueProvider
{
    public Type Type => type ?? typeof(string);

    public Task<object?> GetValueAsync() => ToolPropertyConverterRegistry.ToTargetTypeAsync(value, Type);

    public string? ToInvokeString() => value?.ToString() ?? string.Empty;
}
