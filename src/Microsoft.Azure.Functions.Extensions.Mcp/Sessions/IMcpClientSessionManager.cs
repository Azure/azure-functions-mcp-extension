// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal interface IMcpClientSessionManager
{
    ValueTask<IMcpClientSession<TTransport>> CreateSessionAsync<TTransport>(string clientId, string instanceId, TTransport transport)
        where TTransport : IMcpExtensionTransport;

    ValueTask<GetSessionResult> TryGetSessionAsync(string clientId);

    ValueTask<GetSessionResult<TTransport>> TryGetSessionAsync<TTransport>(string clientId)
        where TTransport : IMcpExtensionTransport;
}

internal record GetSessionResult(bool Succeeded, IMcpClientSession Session)
{
    private static GetSessionResult _notFoundResult = new(false, null!);

    public static GetSessionResult NotFound => _notFoundResult;

    public static GetSessionResult Success(IMcpClientSession session) => new(true, session);
};

internal sealed record GetSessionResult<TTransport>(bool Succeeded, IMcpClientSession<TTransport>? Session)
    where TTransport : IMcpExtensionTransport
{
    public static GetSessionResult<TTransport> NotFound => new(false, null);
    
    public static GetSessionResult<TTransport> Success(IMcpClientSession<TTransport> session) => new(true, session);
};
