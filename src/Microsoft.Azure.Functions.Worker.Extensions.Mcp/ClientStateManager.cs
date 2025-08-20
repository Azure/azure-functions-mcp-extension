// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Manages client state encoding and decoding for MCP sessions
/// </summary>
internal static class ClientStateManager
{
    private static readonly char[] Separator = ['|'];

    /// <summary>
    /// Formats URI state for client session management
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="instanceId">The instance ID</param>
    /// <param name="encrypt">Whether to encrypt the state</param>
    /// <returns>The formatted URI state</returns>
    internal static string FormatUriState(string sessionId, string instanceId, bool encrypt)
    {
        var state = $"{sessionId}|{instanceId}";
        
        if (encrypt)
        {
            // Simple base64 encoding for basic obfuscation
            // In production, consider using proper encryption
            var bytes = Encoding.UTF8.GetBytes(state);
            return Convert.ToBase64String(bytes);
        }
        
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(state));
    }

    /// <summary>
    /// Tries to parse URI state to extract session and instance information
    /// </summary>
    /// <param name="uriState">The encoded URI state</param>
    /// <param name="clientId">The extracted client ID</param>
    /// <param name="instanceId">The extracted instance ID</param>
    /// <param name="encrypted">Whether the state was encrypted</param>
    /// <returns>True if parsing was successful, false otherwise</returns>
    internal static bool TryParseUriState(string? uriState, [NotNullWhen(true)] out string? clientId, [NotNullWhen(true)] out string? instanceId, bool encrypted)
    {
        clientId = null;
        instanceId = null;

        if (string.IsNullOrEmpty(uriState))
        {
            return false;
        }

        try
        {
            var bytes = Convert.FromBase64String(uriState);
            var decoded = Encoding.UTF8.GetString(bytes);
            
            var parts = decoded.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                clientId = parts[0];
                instanceId = parts[1];
                return true;
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return false;
    }
}
