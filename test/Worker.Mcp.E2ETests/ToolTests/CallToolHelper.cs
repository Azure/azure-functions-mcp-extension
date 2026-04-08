// Copyright(c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ToolTests;

/// <summary>
/// Helper class for making server tool invocation requests
/// </summary>
public static class CallToolHelper
{
    /// <summary>
    /// Makes a tool call request to the server endpoint
    /// </summary>
    public static async Task<string> MakeToolCallRequest(Uri appRootEndpoint, object request, ITestOutputHelper testOutputHelper)
    {
        using var httpClient = new HttpClient();

        var json = JsonSerializer.Serialize(request);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Add required MCP headers
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/event-stream");

        var mcpEndpoint = new Uri(appRootEndpoint, "/runtime/webhooks/mcp");

        testOutputHelper.WriteLine($"Making request to {mcpEndpoint}: {json}");

        var response = await httpClient.PostAsync(mcpEndpoint, content);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            testOutputHelper.WriteLine($"Request failed with status {response.StatusCode}: {responseContent}");
            throw new HttpRequestException($"Request failed with status {response.StatusCode}: {responseContent}");
        }

        return responseContent;
    }

    /// <summary>
    /// Creates a JSON-RPC request object for tool calls
    /// </summary>
    public static object CreateToolCallRequest(int id, string toolName, object arguments)
    {
        return new
        {
            jsonrpc = "2.0",
            id,
            method = "tools/call",
            @params = new
            {
                name = toolName,
                arguments
            }
        };
    }
}
