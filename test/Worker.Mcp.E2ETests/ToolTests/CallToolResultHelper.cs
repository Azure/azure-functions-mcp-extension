using System.Text;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ToolTests;

public static class CallToolResultHelper
{
    public static CallToolResult ParseSseToCallToolResult(
        string sseText,
        JsonSerializerOptions? options = null)
    {
        if (sseText is null) throw new ArgumentNullException(nameof(sseText));

        options ??= McpJsonUtilities.DefaultOptions;

        using var reader = new StringReader(sseText);
        string? line;
        var dataBuilder = new StringBuilder();

        while ((line = reader.ReadLine()) is not null)
        {
            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                // accumulate the SSE data payload (may span multiple lines)
                dataBuilder.AppendLine(line.Substring("data:".Length).Trim());
            }
            else if (string.IsNullOrWhiteSpace(line) && dataBuilder.Length > 0)
            {
                // end of first SSE event – this is all we need
                var eventPayload = dataBuilder.ToString().Trim();
                return DeserializeCallToolResult(eventPayload, options);
            }
        }

        // Handle case where there's data but no trailing blank line
        if (dataBuilder.Length > 0)
        {
            var eventPayload = dataBuilder.ToString().Trim();
            return DeserializeCallToolResult(eventPayload, options);
        }

        throw new InvalidOperationException("No SSE data events found in response.");
    }

    private static CallToolResult DeserializeCallToolResult(string eventPayload, JsonSerializerOptions options)
    {
        var jsonRpcResponse =
            JsonSerializer.Deserialize<JsonRpcResponse>(eventPayload, options)
            ?? throw new InvalidOperationException("Unable to deserialize JSON-RPC response from SSE payload.");

        if (jsonRpcResponse.Result is null)
        {
            throw new InvalidOperationException("JSON-RPC response result is null.");
        }

        var callToolResult =
            jsonRpcResponse.Result.Deserialize<CallToolResult>(options)
            ?? throw new InvalidOperationException("Unable to deserialize CallToolResult from JSON-RPC result node.");

        return callToolResult;
    }
}
