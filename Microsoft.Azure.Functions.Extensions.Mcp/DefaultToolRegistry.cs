using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class DefaultToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, IMcpTool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IMcpTool toolListener)
    {
        ArgumentNullException.ThrowIfNull(nameof(toolListener));

        if (!_tools.TryAdd(toolListener.Name, toolListener))
        {
            throw new InvalidOperationException($"Tool with name '{toolListener.Name}' is already registered.");
        }
    }

    public bool TryGetTool(string name, [NotNullWhen(true)] out IMcpTool? tool)
    {
        ArgumentNullException.ThrowIfNull(nameof(name));
        return _tools.TryGetValue(name, out tool);
    }
}