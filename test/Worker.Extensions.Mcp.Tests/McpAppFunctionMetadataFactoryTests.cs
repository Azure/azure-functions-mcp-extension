// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

namespace Worker.Extensions.Mcp.Tests;

public class McpAppFunctionMetadataFactoryTests
{
    [Fact]
    public void CreateViewFunction_SetsCorrectName()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateViewFunction("myTool");

        Assert.Equal("__McpApp_myTool", metadata.Name);
    }

    [Fact]
    public void CreateViewFunction_SetsLanguageToDotnetIsolated()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateViewFunction("myTool");

        Assert.Equal("dotnet-isolated", metadata.Language);
    }

    [Fact]
    public void CreateViewFunction_SetsEntryPoint()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateViewFunction("myTool");

        Assert.Equal(McpAppFunctions.ServeViewEntryPoint, metadata.EntryPoint);
        Assert.Contains("McpAppFunctions", metadata.EntryPoint);
        Assert.Contains("ServeViewAsync", metadata.EntryPoint);
    }

    [Fact]
    public void CreateViewFunction_SetsScriptFile()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateViewFunction("myTool");

        Assert.NotNull(metadata.ScriptFile);
        Assert.EndsWith(".dll", metadata.ScriptFile);
    }

    [Fact]
    public void CreateViewFunction_HasHttpTriggerBinding()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateViewFunction("myTool");

        Assert.Equal(2, metadata.RawBindings!.Count);

        var triggerBinding = JsonNode.Parse(metadata.RawBindings[0])!.AsObject();
        Assert.Equal("httpTrigger", triggerBinding["type"]!.GetValue<string>());
        Assert.Equal("In", triggerBinding["direction"]!.GetValue<string>());
        Assert.Equal("anonymous", triggerBinding["authLevel"]!.GetValue<string>());
        Assert.Equal("mcp/ui/myTool", triggerBinding["route"]!.GetValue<string>());

        var methods = triggerBinding["methods"]!.AsArray();
        Assert.Single(methods);
        Assert.Equal("get", methods[0]!.GetValue<string>());
    }

    [Fact]
    public void CreateViewFunction_HasHttpOutputBinding()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateViewFunction("myTool");

        var outputBinding = JsonNode.Parse(metadata.RawBindings![1])!.AsObject();
        Assert.Equal("http", outputBinding["type"]!.GetValue<string>());
        Assert.Equal("Out", outputBinding["direction"]!.GetValue<string>());
    }

    [Fact]
    public void CreateStaticAssetsFunction_SetsCorrectName()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateStaticAssetsFunction("myTool");

        Assert.Equal("__McpAppAssets_myTool", metadata.Name);
    }

    [Fact]
    public void CreateStaticAssetsFunction_HasCatchAllRoute()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateStaticAssetsFunction("myTool");

        var triggerBinding = JsonNode.Parse(metadata.RawBindings![0])!.AsObject();
        var route = triggerBinding["route"]!.GetValue<string>();
        Assert.Equal("mcp/ui/myTool/assets/{*path}", route);
    }

    [Fact]
    public void CreateStaticAssetsFunction_SetsEntryPointToServeStaticAsset()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateStaticAssetsFunction("myTool");

        Assert.Equal(McpAppFunctions.ServeStaticAssetEntryPoint, metadata.EntryPoint);
        Assert.Contains("ServeStaticAssetAsync", metadata.EntryPoint);
    }

    [Fact]
    public void CreateViewFunction_RouteContainsToolName()
    {
        var metadata = McpAppFunctionMetadataFactory.CreateViewFunction("data_explorer");

        var triggerBinding = JsonNode.Parse(metadata.RawBindings![0])!.AsObject();
        Assert.Equal("mcp/ui/data_explorer", triggerBinding["route"]!.GetValue<string>());
    }
}
