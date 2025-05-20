using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Functions.Extensions.Mcp.Configuration;
using Moq;
using Microsoft.Azure.WebJobs.Extensions.Mcp;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class DefaultRequestHandlerTests
{
    private readonly IMcpInstanceIdProvider _instanceIdProvider;

    public DefaultRequestHandlerTests()
    {
        var idProviderMock = new Mock<IMcpInstanceIdProvider>();
        idProviderMock.Setup(p => p.InstanceId)
            .Returns("instance");
        _instanceIdProvider = idProviderMock.Object;
    }

    private static IOptions<McpOptions> CreateOptions(bool useAbsoluteUri)
    {
        return Options.Create(new McpOptions
        {
            EncryptClientState = false,
            MessageOptions = new MessageOptions { UseAbsoluteUriForEndpoint = useAbsoluteUri }
        });
    }

    [Fact]
    public void WriteEndpoint_ReturnsRelativeUri_WhenUseAbsoluteUriForEndpointIsFalse()
    {
        var options = CreateOptions(false);
        var handler = new DefaultRequestHandler(null!, _instanceIdProvider, options, NullLogger<Logs.DefaultRequestHandler>.Instance);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/api/sse",
                PathBase = ""
            }
        };

        var endpoint = handler.WriteEndpoint("client", context);

        Assert.StartsWith("message?azmcpcs=", endpoint);
        Assert.DoesNotContain("http://", endpoint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WriteEndpoint_ReturnsAbsoluteUri_WhenUseAbsoluteUriForEndpointIsTrue()
    {
        var options = CreateOptions(true);
        var handler = new DefaultRequestHandler(null!, _instanceIdProvider, options, NullLogger<Logs.DefaultRequestHandler>.Instance);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Scheme = "https",
                Host = new HostString("example.com"),
                Path = "/runtime/webhooks/sse",
                PathBase = ""
            }
        };

        var endpoint = handler.WriteEndpoint("client", context);
        Assert.StartsWith("https://example.com/runtime/webhooks/message?azmcpcs=", endpoint);
    }

    [Fact]
    public void WriteEndpoint_AppendsFunctionKey_WhenPresent()
    {
        var options = CreateOptions(false);
        var handler = new DefaultRequestHandler(null!, _instanceIdProvider, options, NullLogger<Logs.DefaultRequestHandler>.Instance);
        var context = new DefaultHttpContext
        {
            Request =
            {
                QueryString = new QueryString("?code=abc123"),
                Query = new QueryCollection(new Dictionary<string, StringValues> { { "code", new StringValues("abc123") } })
            }
        };

        var endpoint = handler.WriteEndpoint("client", context);

        Assert.Contains("code=abc123", endpoint);
    }
}