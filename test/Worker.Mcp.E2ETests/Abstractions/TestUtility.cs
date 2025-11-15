// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Abstractions;

public static class TestUtility
{
    public static async Task RetryAsync(Func<Task<bool>> condition, int timeout = 60 * 1000, int pollingInterval = 2 * 1000, bool throwWhenDebugging = false, Func<string>? userMessageCallback = null)
    {
        DateTime start = DateTime.Now;
        while (!await condition())
        {
            await Task.Delay(pollingInterval);

            bool shouldThrow = !Debugger.IsAttached || Debugger.IsAttached && throwWhenDebugging;
            if (shouldThrow && (DateTime.Now - start).TotalMilliseconds > timeout)
            {
                string error = "Condition not reached within timeout.";
                if (userMessageCallback != null)
                {
                    error += " " + userMessageCallback();
                }
                throw new ApplicationException(error);
            }
        }
    }

    /// <summary>
    /// Waits for the Azure Functions host to be in a running state with additional process monitoring
    /// </summary>
    /// <param name="appRootEndpoint">The root endpoint of the Azure Functions host</param>
    /// <param name="process">The Functions host process to monitor</param>
    /// <param name="logger">Optional logger for diagnostic information</param>
    /// <param name="timeout">Timeout in milliseconds (default: 60 seconds)</param>
    /// <param name="pollingInterval">Polling interval in milliseconds (default: 2 seconds)</param>
    /// <returns>Task that completes when the host is running</returns>
    public static async Task WaitForFunctionsHostToBeRunningAsync(
        Uri appRootEndpoint,
        Process process,
        ILogger? logger = null,
        int timeout = 60000,
        int pollingInterval = 2000)
    {
        using var httpClient = new HttpClient();
        logger?.LogInformation("Waiting for Azure Functions host to be running...");

        await RetryAsync(async () =>
        {
            try
            {
                var response = await httpClient.GetAsync(new Uri(appRootEndpoint, "admin/host/status"));
                var content = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("state", out JsonElement value) &&
                    value.GetString() == "Running")
                {
                    logger?.LogInformation("  Azure Functions host state: Running");
                    return true;
                }

                logger?.LogInformation($"  Azure Functions host state: {value}");
                return false;
            }
            catch (Exception ex)
            {
                if (process.HasExited)
                {
                    // Something went wrong starting the host - check the logs
                    logger?.LogError($"  Azure Functions host process exited unexpectedly with code {process.ExitCode}");
                    throw new InvalidOperationException($"Azure Functions host process exited unexpectedly with code {process.ExitCode}");
                }

                // Can get exceptions before host is running.
                logger?.LogInformation($"  Azure Functions host state: Starting (Exception: {ex.Message})");
                return false;
            }
        }, timeout, pollingInterval);
    }
}
