// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal class ClientStateManager
{
    private static readonly bool _isCoreToolsEnvironment = Environment.GetEnvironmentVariable("FUNCTIONS_CORETOOLS_ENVIRONMENT") is not null;

    public static bool TryParseUriState(string clientState, [NotNullWhen(true)] out string? clientId, [NotNullWhen(true)] out string? instanceId, bool isEncrypted = true)
    {
        // If not encrypted, or running locally, we use a plain client state format
        if (isEncrypted && !_isCoreToolsEnvironment)
        {
            clientState = TokenUtility.ReadUriState(clientState);
        }

        return TryParsePlainClientState(clientState, out clientId, out instanceId);
    }

    public static string FormatUriState(string clientId, string instanceId, bool isEncrypted = true)
    {
        var uriState = $"{clientId}|{instanceId}";

        // If not encrypted, or running locally, we use a plain client state format
        if (!isEncrypted || _isCoreToolsEnvironment)
        {
            return uriState;
        }

        return TokenUtility.ProtectUriState(uriState);
    }

    private static bool TryParsePlainClientState(string clientState, [NotNullWhen(true)] out string? clientId, [NotNullWhen(true)] out string? instanceId)
    {
        clientId = null;
        instanceId = null;

        if (string.IsNullOrEmpty(clientState))
        {
            return false;
        }

        var parts = clientState.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        clientId = parts[0];
        instanceId = parts[1];

        return true;
    }
}
