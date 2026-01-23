// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.Functions.Extensions.Mcp.Validation;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class DefaultToolRegistryTests
{
    [Fact]
    public async Task ListToolsAsync_PreservesMetadata()
    {
        var registry = new DefaultToolRegistry();

        var metadata = new JsonObject
        {
            ["openai/outputTemplate"] = "ui://widget/weather.html"
        };

        using var schemaDoc = JsonDocument.Parse("""{"type":"object"}""");
        var inputSchema = new JsonSchemaToolInputSchema(schemaDoc);

        var tool = new Mock<IMcpTool>();
        tool.SetupGet(t => t.Name).Returns("weather");
        tool.SetupProperty(t => t.Description, "Weather tool");
        tool.SetupGet(t => t.ToolInputSchema).Returns(inputSchema);
        tool.SetupGet(t => t.Metadata).Returns(metadata);
        tool.Setup(t => t.RunAsync(It.IsAny<RequestContext<CallToolRequestParams>>(), It.IsAny<CancellationToken>()))
            .Throws<NotImplementedException>();

        registry.Register(tool.Object);

        var result = await registry.ListToolsAsync();

        var returnedTool = Assert.Single(result.Tools);
        Assert.Equal(metadata, returnedTool.Meta);
    }
}
