// Copyright(c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ResourceTests;

/// <summary>
/// Helper class for making server resource reading requests
/// </summary>
public static class ResourceHelper
{
    /// <summary>
    /// Makes a resource request to the server endpoint
    /// </summary>
    public static async Task<string> MakeResourceRequest(Uri appRootEndpoint, object request, ITestOutputHelper testOutputHelper)
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
    /// Creates a JSON-RPC request object for resource/read
    /// </summary>
    public static object CreateResourceReadRequest(int id, string uri)
    {
        return new
        {
            jsonrpc = "2.0",
            id,
            method = "resources/read",
            @params = new
            {
                uri
            }
        };
    }

    /// <summary>
    /// Creates a JSON-RPC request object for resources/list
    /// </summary>
    public static object CreateResourceListRequest(int id)
    {
        return new
        {
            jsonrpc = "2.0",
            id,
            method = "resources/list"
        };
    }

    /// <summary>
    /// Extracts JSON from Server-Sent Events format response.
    /// SSE format: "event: message\ndata: {json}"
    /// </summary>
    public static string ExtractJsonFromSSE(string sseResponse)
    {
        var lines = sseResponse.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        foreach (var line in lines)
        {
            if (line.StartsWith("data: "))
            {
                return line.Substring("data: ".Length);
            }
        }

        // If no SSE format found, assume it's plain JSON
        return sseResponse;
    }
}
