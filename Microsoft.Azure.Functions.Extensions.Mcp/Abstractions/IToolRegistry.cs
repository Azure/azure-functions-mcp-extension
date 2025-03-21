using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Model;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;

internal interface IToolRegistry
{
    void Register(IMcpTool toolListener);

    bool TryGetTool(string name, [NotNullWhen(true)] out IMcpTool? tool);

    ICollection<IMcpTool> GetTools();
}