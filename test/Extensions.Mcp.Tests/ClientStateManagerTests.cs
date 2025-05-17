using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class ClientStateManagerTests
{
    [Fact]
    public void FormatUriState_And_TryParseUriState_RoundTrip_Success()
    {
        var clientId = "client";
        var instanceId = "instance";
        var state = ClientStateManager.FormatUriState(clientId, instanceId, isEncrypted: false);
        
        Assert.True(ClientStateManager.TryParseUriState(state, out var parsedClientId, out var parsedInstanceId, isEncrypted: false));
        Assert.Equal(clientId, parsedClientId);
        Assert.Equal(instanceId, parsedInstanceId);
    }

    [Fact]
    public void TryParseUriState_InvalidFormat_ReturnsFalse()
    {
        Assert.False(ClientStateManager.TryParseUriState("", out _, out _, isEncrypted: false));
        Assert.False(ClientStateManager.TryParseUriState("onlyonepart", out _, out _, isEncrypted: false));
        Assert.False(ClientStateManager.TryParseUriState("a|b|c", out _, out _, isEncrypted: false));
        Assert.False(ClientStateManager.TryParseUriState("valid|format|extra", out _, out _, isEncrypted: false));
        
        Assert.True(ClientStateManager.TryParseUriState("client|instance", out var parsedClientId, out var parsedInstanceId, isEncrypted: false));
        Assert.Equal("client", parsedClientId);
        Assert.Equal("instance", parsedInstanceId);
        
        Assert.False(ClientStateManager.TryParseUriState("invalidformat", out _, out _, isEncrypted: false));
    }
}
