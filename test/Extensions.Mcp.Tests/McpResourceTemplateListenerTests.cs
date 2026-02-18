// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using Microsoft.Azure.WebJobs.Host.Executors;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class McpResourceTemplateListenerTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var metadata = new Dictionary<string, object?> { { "key1", "value1" } };
        var templateRegex = new Regex("^file://(?<filename>[^?#]+)$");

        var listener = new McpResourceTemplateListener(
            executor,
            "MyFunction",
            "file://{filename}",
            "FileResource",
            "Read files",
            "text/plain",
            null,
            metadata,
            templateRegex);

        Assert.Equal("MyFunction", listener.FunctionName);
        Assert.Equal("file://{filename}", listener.Uri);
        Assert.Equal("FileResource", listener.Name);
        Assert.Equal("Read files", listener.Description);
        Assert.Equal("text/plain", listener.MimeType);
        Assert.Same(metadata, listener.Metadata);
        Assert.Same(templateRegex, listener.TemplateRegex);
    }

    [Fact]
    public void ImplementsIMcpResourceTemplate()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var templateRegex = new Regex("^file://(?<filename>[^?#]+)$");

        var listener = new McpResourceTemplateListener(
            executor,
            "MyFunction",
            "file://{filename}",
            "FileResource",
            null,
            null,
            null,
            new Dictionary<string, object?>(),
            templateRegex);

        Assert.IsAssignableFrom<IMcpResource>(listener);
        Assert.IsAssignableFrom<IMcpResourceTemplate>(listener);
    }

    [Fact]
    public void TemplateRegex_MatchesExpectedUris()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var templateRegex = ResourceUriHelper.BuildTemplateRegex("file://{filename}");

        var listener = new McpResourceTemplateListener(
            executor,
            "MyFunction",
            "file://{filename}",
            "FileResource",
            null,
            null,
            null,
            new Dictionary<string, object?>(),
            templateRegex);

        Assert.Matches(listener.TemplateRegex, "file://welcome.html");
        Assert.Matches(listener.TemplateRegex, "file://readme.txt");
        Assert.DoesNotMatch(listener.TemplateRegex, "http://other/file.txt");
    }

    [Fact]
    public async Task StartAsync_Completes()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var templateRegex = new Regex("^file://(?<filename>[^?#]+)$");
        var listener = new McpResourceTemplateListener(
            executor,
            "MyFunction",
            "file://{filename}",
            "FileResource",
            null,
            null,
            null,
            new Dictionary<string, object?>(),
            templateRegex);

        await listener.StartAsync(CancellationToken.None);
        // Should complete without throwing
    }

    [Fact]
    public async Task StopAsync_Completes()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var templateRegex = new Regex("^file://(?<filename>[^?#]+)$");
        var listener = new McpResourceTemplateListener(
            executor,
            "MyFunction",
            "file://{filename}",
            "FileResource",
            null,
            null,
            null,
            new Dictionary<string, object?>(),
            templateRegex);

        await listener.StopAsync(CancellationToken.None);
        // Should complete without throwing
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var executor = new Mock<ITriggeredFunctionExecutor>().Object;
        var templateRegex = new Regex("^file://(?<filename>[^?#]+)$");
        var listener = new McpResourceTemplateListener(
            executor,
            "MyFunction",
            "file://{filename}",
            "FileResource",
            null,
            null,
            null,
            new Dictionary<string, object?>(),
            templateRegex);

        listener.Dispose();
        // Should complete without throwing
    }
}
