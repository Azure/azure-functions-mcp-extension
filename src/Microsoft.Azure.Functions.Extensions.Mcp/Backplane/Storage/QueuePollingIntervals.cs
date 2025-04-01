namespace Microsoft.Azure.Functions.Extensions.Mcp.Backplane.Storage;

internal static class QueuePollingIntervals
{
    public static readonly TimeSpan Minimum = TimeSpan.FromMilliseconds(100);
    public static readonly TimeSpan DefaultMaximum = TimeSpan.FromMinutes(1);
}
