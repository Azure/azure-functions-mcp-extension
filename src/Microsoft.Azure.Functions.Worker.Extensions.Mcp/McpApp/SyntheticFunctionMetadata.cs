// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// A minimal <see cref="IFunctionMetadata"/> implementation used to emit synthetic
/// function descriptors (e.g., for MCP App resource triggers) without a real user function.
/// </summary>
internal sealed class SyntheticFunctionMetadata(string name, string bindingJson) : IFunctionMetadata
{
    private static readonly string ExtensionAssemblyFile =
        Path.GetFileName(typeof(SyntheticFunctionMetadata).Assembly.Location);

    public string? Name { get; set; } = name;
    public IList<string>? RawBindings { get; set; } = [bindingJson];
    public string? EntryPoint { get; set; } = "Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration.SyntheticFunctionMetadata.Run";

    // The host validates that ScriptFile points to a real file before binding so we need to provide one.
    public string? ScriptFile { get; set; } = ExtensionAssemblyFile;
    public string? FunctionId { get; set; } = Guid.NewGuid().ToString();
    public bool IsProxy { get; set; } = false;
    public string? Language { get; set; } = "dotnet-isolated";
    public bool? ManagedDependencyEnabled { get; set; }
    public IRetryOptions? Retry { get; set; }

    bool IFunctionMetadata.ManagedDependencyEnabled => false;

    public void Run()
    {
        // No-op: This method will never actually be invoked since the host doesn't load any real function code for synthetic metadata.
        throw new NotImplementedException("This method is not implemented because SyntheticFunctionMetadata is only used for emitting function descriptors, not for executing real functions.");
    }
}
