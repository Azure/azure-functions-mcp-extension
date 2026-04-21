// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.Builders.Steps;
using static Worker.Extensions.Mcp.Tests.Helpers.BuilderTestHelper;

namespace Worker.Extensions.Mcp.Tests.Configuration;

public class ResolveToolOutputSchemaExtensionTests
{
    private const string ValidSchema = """{"type":"object","properties":{"x":{"type":"string"}},"required":["x"]}""";
    private const string ToolBinding = """{"type":"mcpToolTrigger","toolName":"MyTool"}""";

    [Fact]
    public void ResolveToolOutputSchema_NoExplicitSchema_DoesNothing()
    {
        var builder = CreateBuilder(CreateToolOptions("MyTool"), CreateResourceOptions(), CreatePromptOptions(), ToolBinding);

        builder.ResolveToolOutputSchema();

        var binding = builder.Context.Bindings[0];
        Assert.Null(binding.OutputSchema);
    }

    [Fact]
    public void ResolveToolOutputSchema_ExplicitSchema_AppliesSchema()
    {
        var toolOptions = CreateToolOptions("MyTool", outputSchema: ValidSchema);
        var builder = CreateBuilder(toolOptions, CreateResourceOptions(), CreatePromptOptions(), ToolBinding);

        builder.ResolveToolOutputSchema();

        var binding = builder.Context.Bindings[0];
        Assert.Equal(ValidSchema, binding.OutputSchema);
    }

    [Fact]
    public void ResolveToolOutputSchema_ExplicitSchema_FlowsThroughBuildToRawBinding()
    {
        var toolOptions = CreateToolOptions("MyTool", outputSchema: ValidSchema);
        var builder = CreateBuilder(toolOptions, CreateResourceOptions(), CreatePromptOptions(), ToolBinding);

        builder.ResolveToolOutputSchema().Build();

        var raw = builder.Context.Function.RawBindings![0];
        Assert.Contains("\"outputSchema\":", raw);
    }

    [Fact]
    public void ResolveToolOutputSchema_InvalidSchemaInOptions_LogsAndSkips()
    {
        var toolOptions = CreateToolOptions("MyTool", outputSchema: "{not valid json");
        var builder = CreateBuilder(toolOptions, CreateResourceOptions(), CreatePromptOptions(), ToolBinding);

        builder.ResolveToolOutputSchema();

        var binding = builder.Context.Bindings[0];
        Assert.Null(binding.OutputSchema);
    }

    [Fact]
    public void ResolveToolOutputSchema_NonToolBinding_Ignored()
    {
        var toolOptions = CreateToolOptions("MyTool", outputSchema: ValidSchema);
        var builder = CreateBuilder(toolOptions, CreateResourceOptions(), CreatePromptOptions(),
            """{"type":"mcpResourceTrigger","uri":"file://x"}""");

        builder.ResolveToolOutputSchema();

        var binding = builder.Context.Bindings[0];
        Assert.Null(binding.OutputSchema);
    }

    [Fact]
    public void ResolveToolOutputSchema_ReturnsSameBuilder_ForChaining()
    {
        var builder = CreateBuilder(ToolBinding);

        var result = builder.ResolveToolOutputSchema();

        Assert.Same(builder, result);
    }
}
