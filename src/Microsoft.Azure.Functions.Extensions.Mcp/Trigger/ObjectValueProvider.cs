// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal class ObjectValueProvider : IValueProvider // I don't think I understand the flow of ObjectValueProvider -> ToolPropertyValueProvider
{
    private readonly object? _value;
    private readonly Task<object?> _valueAsTask;

    public ObjectValueProvider(object? value, Type valueType)
    {
        ArgumentNullException.ThrowIfNull(valueType);

        if (value != null && !valueType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"Cannot convert {value} to {valueType.Name}.");
        }

        _value = value;
        _valueAsTask = Task.FromResult(value);
        Type = valueType;
    }

    public Type Type { get; }

    public Task<object?> GetValueAsync() => _valueAsTask;

    public string? ToInvokeString() => _value?.ToString(); // Who calls this? And what is it used for?
}
