namespace Microsoft.Azure.Functions.Extensions.Mcp;

public interface IToolProvider
{
    Task<object> RunToolAsync(string name, CancellationToken cancellationToken);
}