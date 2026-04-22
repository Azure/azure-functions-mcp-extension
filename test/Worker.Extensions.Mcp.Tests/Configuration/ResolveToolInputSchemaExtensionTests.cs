// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;
using static Worker.Extensions.Mcp.Tests.Helpers.BuilderTestHelper;

namespace Worker.Extensions.Mcp.Tests.Configuration;

public class ResolveToolInputSchemaExtensionTests
{
    private const string ValidSchema = """{"type":"object","properties":{"x":{"type":"string"}},"required":["x"]}""";
    private const string ToolBinding = """{"type":"mcpToolTrigger","toolName":"MyTool"}""";

    [Fact]
    public void ResolveToolInputSchema_NoExplicitSchema_DoesNothing()
    {
        var builder = CreateBuilder(CreateToolOptions("MyTool"), CreateResourceOptions(), CreatePromptOptions(), ToolBinding);

        builder.ResolveToolInputSchema();

        var binding = builder.Context.Bindings[0];
        Assert.Null(binding.InputSchema);
        Assert.False(binding.UseWorkerInputSchema);
    }

    [Fact]
    public void ResolveToolInputSchema_ExplicitSchema_AppliesSchemaAndFlag()
    {
        var toolOptions = CreateToolOptions("MyTool", inputSchema: ValidSchema);
        var builder = CreateBuilder(toolOptions, CreateResourceOptions(), CreatePromptOptions(), ToolBinding);

        builder.ResolveToolInputSchema();

        var binding = builder.Context.Bindings[0];
        Assert.Equal(ValidSchema, binding.InputSchema);
        Assert.True(binding.UseWorkerInputSchema);
    }

    [Fact]
    public void ResolveToolInputSchema_ExplicitSchema_FlowsThroughBuildToRawBinding()
    {
        var toolOptions = CreateToolOptions("MyTool", inputSchema: ValidSchema);
        var builder = CreateBuilder(toolOptions, CreateResourceOptions(), CreatePromptOptions(), ToolBinding);

        builder.ResolveToolInputSchema().Build();

        // Build mutates the underlying RawBindings list passed into the function metadata.
        var raw = builder.Context.Function.RawBindings![0];
        Assert.Contains("\"inputSchema\":", raw);
        Assert.Contains("\"useWorkerInputSchema\":true", raw);
    }

    [Fact]
    public void ResolveToolInputSchema_NonToolBinding_Ignored()
    {
        var toolOptions = CreateToolOptions("MyTool", inputSchema: ValidSchema);
        var builder = CreateBuilder(toolOptions, CreateResourceOptions(), CreatePromptOptions(),
            """{"type":"mcpResourceTrigger","uri":"file://x"}""");

        builder.ResolveToolInputSchema();

        var binding = builder.Context.Bindings[0];
        Assert.Null(binding.InputSchema);
        Assert.False(binding.UseWorkerInputSchema);
    }

    [Fact]
    public void ResolveToolInputSchema_ReturnsSameBuilder_ForChaining()
    {
        var builder = CreateBuilder(ToolBinding);

        var result = builder.ResolveToolInputSchema();

        Assert.Same(builder, result);
    }

    [Fact]
    public void ResolveToolInputSchema_NullBuilder_Throws()
    {
        McpBindingBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.ResolveToolInputSchema());
    }
}
