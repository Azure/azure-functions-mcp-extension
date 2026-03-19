// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests;

public class McpAppFunctionMetadataFactoryTests
{
    [Fact]
    public void CreateViewResourceFunction_SetsCorrectName()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateViewResourceFunction("myTool");

        Assert.Equal("functions--mcpapp-myTool", metadata.Name);
    }

    [Fact]
    public void CreateViewResourceFunction_SetsLanguageToDotnetIsolated()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateViewResourceFunction("myTool");

        Assert.Equal("dotnet-isolated", metadata.Language);
    }

    [Fact]
    public void CreateViewResourceFunction_SetsEntryPoint()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateViewResourceFunction("myTool");

        Assert.Equal(McpAppFunctions.ServeViewEntryPoint, metadata.EntryPoint);
        Assert.Contains("McpAppFunctions", metadata.EntryPoint);
        Assert.Contains("ServeView", metadata.EntryPoint);
    }

    [Fact]
    public void CreateViewResourceFunction_SetsScriptFile()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateViewResourceFunction("myTool");

        Assert.NotNull(metadata.ScriptFile);
        Assert.EndsWith(".dll", metadata.ScriptFile);
    }

    [Fact]
    public void CreateViewResourceFunction_HasMcpResourceTriggerBinding()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateViewResourceFunction("myTool");

        Assert.Single(metadata.RawBindings!);

        var triggerBinding = JsonNode.Parse(metadata.RawBindings![0])!.AsObject();
        Assert.Equal("mcpResourceTrigger", triggerBinding["type"]!.GetValue<string>());
        Assert.Equal("In", triggerBinding["direction"]!.GetValue<string>());
        Assert.Equal("ui://myTool/view", triggerBinding["uri"]!.GetValue<string>());
        Assert.Equal("text/html;profile=mcp-app", triggerBinding["mimeType"]!.GetValue<string>());
    }

    [Fact]
    public void CreateViewResourceFunction_ResourceNameContainsToolName()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateViewResourceFunction("data_explorer");

        var triggerBinding = JsonNode.Parse(metadata.RawBindings![0])!.AsObject();
        Assert.Equal("ui://data_explorer/view", triggerBinding["uri"]!.GetValue<string>());
        Assert.Equal("data_explorer_view", triggerBinding["resourceName"]!.GetValue<string>());
    }
}
