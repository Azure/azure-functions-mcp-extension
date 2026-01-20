// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace Worker.Extensions.Mcp.Tests;

public class ToolInvocationContextExtensionsTests
{
    [Fact]
    public void TryGetHttpTransport_ReturnsTrue_WhenHttpTransportPresent()
    {
        var http = new HttpTransport("http-streamable");
        var context = new ToolInvocationContext
        {
            Name = "toolA",
            Transport = http
        };

        var result = context.TryGetHttpTransport(out var extracted);

        Assert.True(result);
        Assert.NotNull(extracted);
        Assert.Same(http, extracted);
        Assert.Equal(HttpTransportType.Streamable, extracted!.Type);
    }

    [Fact]
    public void TryGetHttpTransport_ReturnsFalse_WhenTransportIsNull()
    {
        var context = new ToolInvocationContext
        {
            Name = "toolB"
        };

        var result = context.TryGetHttpTransport(out var extracted);

        Assert.False(result);
        Assert.Null(extracted);
    }

    private sealed class CustomTransport : Transport
    {
        public CustomTransport(string name) => Name = name;
    }

    [Fact]
    public void TryGetHttpTransport_ReturnsFalse_WhenTransportIsDifferentSubclass()
    {
        var context = new ToolInvocationContext
        {
            Name = "toolC",
            Transport = new CustomTransport("custom")
        };

        var result = context.TryGetHttpTransport(out var extracted);

        Assert.False(result);
        Assert.Null(extracted);
    }

    /*
    [Fact]
    public void TryGetHttpTransport_ThrowsNullReference_WhenContextIsNull()
    {
        ToolInvocationContext? context = null;

        Assert.Throws<NullReferenceException>(() =>
        {
            ToolInvocationContextExtensions.TryGetHttpTransport(context!, out _);
        });
    }
    */
}
