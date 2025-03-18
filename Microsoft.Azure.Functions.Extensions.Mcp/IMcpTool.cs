namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IMcpTool
{
    string Name { get; }

    Task<object> RunAsync(CancellationToken cancellationToken);
}