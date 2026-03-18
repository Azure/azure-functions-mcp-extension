// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Represents the content source for an MCP App view.
/// This is a discriminated union — exactly one concrete subtype is used per view.
/// </summary>
public abstract record McpViewSource
{
    // Prevent external subclassing
    internal McpViewSource() { }

    /// <summary>
    /// Creates a view source backed by a file on disk.
    /// </summary>
    /// <param name="path">
    /// The path to the HTML file, relative to the application root or absolute.
    /// </param>
    public static McpViewSource FromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new FileViewSource(path);
    }

    /// <summary>
    /// Creates a view source backed by a manifest resource embedded in an assembly.
    /// </summary>
    /// <param name="resourceName">
    /// The fully qualified manifest resource name (e.g., "MyLib.Resources.view.html").
    /// </param>
    /// <param name="assembly">
    /// The assembly containing the resource. Defaults to the calling assembly.
    /// </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static McpViewSource FromEmbeddedResource(string resourceName, Assembly? assembly = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        return new EmbeddedViewSource(assembly ?? Assembly.GetCallingAssembly(), resourceName);
    }
}

/// <summary>
/// A view source backed by a file on disk.
/// </summary>
/// <param name="Path">The path to the HTML file.</param>
public sealed record FileViewSource(string Path) : McpViewSource;

/// <summary>
/// A view source backed by a manifest resource embedded in an assembly.
/// </summary>
/// <param name="Assembly">The assembly containing the resource.</param>
/// <param name="ResourceName">The fully qualified manifest resource name.</param>
public sealed record EmbeddedViewSource(Assembly Assembly, string ResourceName) : McpViewSource;
