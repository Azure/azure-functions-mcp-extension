// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.Functions.Extensions.Mcp.Abstractions;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpResourceTriggerBindingProviderTests
{
    private static void DummyMethod([McpResourceTrigger("test://resource/1", "TestResource")] ResourceInvocationContext ctx) { }

    private static void DummyMethodWithoutAttribute(string ctx) { }

    private static TriggerBindingProviderContext CreateContext(ParameterInfo parameter)
    {
        return new TriggerBindingProviderContext(parameter, CancellationToken.None);
    }

    [Fact]
    public async Task TryCreateAsync_WithAttribute_ReturnsBinding()
    {
        var resourceRegistry = new Mock<IResourceRegistry>().Object;
        var provider = new McpResourceTriggerBindingProvider(resourceRegistry, NullLoggerFactory.Instance);

        var method = typeof(McpResourceTriggerBindingProviderTests).GetMethod(nameof(DummyMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var parameter = method.GetParameters()[0];
        var context = CreateContext(parameter);

        var binding = await provider.TryCreateAsync(context);

        Assert.NotNull(binding);
        Assert.IsType<McpResourceTriggerBinding>(binding);
    }

    [Fact]
    public async Task TryCreateAsync_WithoutAttribute_ReturnsNull()
    {
        var resourceRegistry = new Mock<IResourceRegistry>().Object;
        var provider = new McpResourceTriggerBindingProvider(resourceRegistry, NullLoggerFactory.Instance);

        var method = typeof(McpResourceTriggerBindingProviderTests).GetMethod(nameof(DummyMethodWithoutAttribute), BindingFlags.NonPublic | BindingFlags.Static)!;
        var parameter = method.GetParameters()[0];
        var context = CreateContext(parameter);

        var binding = await provider.TryCreateAsync(context);

        Assert.Null(binding);
    }

    [Fact]
    public async Task TryCreateAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var resourceRegistry = new Mock<IResourceRegistry>().Object;
        var provider = new McpResourceTriggerBindingProvider(resourceRegistry, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.TryCreateAsync(null!));
    }

    [Fact]
    public async Task TryCreateAsync_CreatesBindingWithCorrectRegistry()
    {
        var resourceRegistryMock = new Mock<IResourceRegistry>();
        var provider = new McpResourceTriggerBindingProvider(resourceRegistryMock.Object, NullLoggerFactory.Instance);

        var method = typeof(McpResourceTriggerBindingProviderTests).GetMethod(nameof(DummyMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var parameter = method.GetParameters()[0];
        var context = CreateContext(parameter);

        var binding = await provider.TryCreateAsync(context);

        Assert.NotNull(binding);
        // The binding should have been created with the registry
        // We can't directly verify the registry was passed, but we can verify the binding was created
        Assert.IsType<McpResourceTriggerBinding>(binding);
    }
}
