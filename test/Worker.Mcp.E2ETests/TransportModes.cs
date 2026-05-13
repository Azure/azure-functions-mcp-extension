// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Client;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests;

/// <summary>
/// Shared transport-mode matrices for e2e Theories.
/// AutoDetect currently resolves to the same SSE endpoint as Sse in the test
/// fixture, so it is intentionally excluded from the default matrix to avoid
/// duplicate coverage. Add explicit AutoDetect tests only when the behaviour
/// under test depends on the auto-detection code path itself.
/// </summary>
internal static class TransportModes
{
    public static TheoryData<HttpTransportMode> All => new()
    {
        HttpTransportMode.Sse,
        HttpTransportMode.StreamableHttp,
    };

    public static TheoryData<HttpTransportMode> StreamableHttpOnly => new()
    {
        HttpTransportMode.StreamableHttp,
    };
}
