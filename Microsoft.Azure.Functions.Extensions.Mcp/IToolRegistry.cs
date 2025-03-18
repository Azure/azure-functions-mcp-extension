using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IToolRegistry
{
    void Register(IMcpTool toolListener);

    bool TryGetTool(string name, [NotNullWhen(true)] out IMcpTool? tool);
}