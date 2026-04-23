// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Mcp.Validation;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Tests;

public class DefaultToolRegistryTests
{
    [Fact]
    public async Task ListToolsAsync_PropagatesOutputSchemaToProtocolResponse()
    {
        const string schemaJson = """
            {
              "type": "object",
              "properties": { "temperature": { "type": "number" } },
              "required": ["temperature"]
            }
            """;
        using var schemaDoc = JsonDocument.Parse(schemaJson);
        var outputSchema = schemaDoc.RootElement.Clone();

        var registry = new DefaultToolRegistry();
        registry.Register(CreateTool("tool-with-output", outputSchema));

        var result = await registry.ListToolsAsync();

        var tool = Assert.Single(result.Tools);
        Assert.Equal("tool-with-output", tool.Name);
        Assert.NotNull(tool.OutputSchema);
        Assert.Equal("object", tool.OutputSchema.Value.GetProperty("type").GetString());
        Assert.True(tool.OutputSchema.Value.GetProperty("properties").TryGetProperty("temperature", out _));
    }

    [Fact]
    public async Task ListToolsAsync_WithoutOutputSchema_LeavesProtocolFieldNull()
    {
        var registry = new DefaultToolRegistry();
        registry.Register(CreateTool("tool-without-output", outputSchema: null));

        var result = await registry.ListToolsAsync();

        var tool = Assert.Single(result.Tools);
        Assert.Equal("tool-without-output", tool.Name);
        Assert.Null(tool.OutputSchema);
    }

    private static IMcpTool CreateTool(string name, JsonElement? outputSchema)
    {
        var mock = new Mock<IMcpTool>();
        mock.SetupGet(t => t.Name).Returns(name);
        mock.SetupProperty(t => t.Description, "desc");
        mock.SetupGet(t => t.ToolInputSchema).Returns(new PropertyBasedToolInputSchema([]));
        mock.SetupGet(t => t.OutputSchema).Returns(outputSchema);
        mock.SetupGet(t => t.Metadata).Returns(new Dictionary<string, object?>());
        mock.Setup(t => t.RunAsync(It.IsAny<RequestContext<CallToolRequestParams>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CallToolResult());
        return mock.Object;
    }
}
