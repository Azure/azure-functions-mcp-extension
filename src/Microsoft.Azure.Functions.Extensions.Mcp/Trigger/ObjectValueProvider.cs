using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal class ObjectValueProvider : IValueProvider
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

    public string? ToInvokeString() => _value?.ToString();
}