using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public class ToolPropertyValueProvider(string? value) : IValueProvider
{
    public Type Type => typeof(string);

    public Task<object?> GetValueAsync() => Task.FromResult<object?>(value);

    public string? ToInvokeString() => value;
}