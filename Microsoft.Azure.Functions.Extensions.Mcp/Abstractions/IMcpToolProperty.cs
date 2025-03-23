namespace Microsoft.Azure.Functions.Extensions.Mcp;

public interface IMcpToolProperty
{
    public string PropertyName { get; set; }

    public string PropertyType { get; set; }

    public string? Description { get; set; }
}
