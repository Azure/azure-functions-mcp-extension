// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// Resolves function methods from function metadata using reflection.
/// </summary>
internal static partial class FunctionMethodResolver
{
    private const string FunctionsWorkerDirectoryKey = "FUNCTIONS_WORKER_DIRECTORY";
    private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";

    /// <summary>
    /// Attempts to resolve the method for a function from its metadata.
    /// </summary>
    public static bool TryResolveMethod(IFunctionMetadata functionMetadata, [NotNullWhen(true)] out MethodInfo? method)
    {
        method = null;

        var match = GetEntryPointRegex().Match(functionMetadata.EntryPoint ?? string.Empty);
        if (!match.Success)
        {
            return false;
        }

        var typeName = match.Groups["typename"].Value;
        var methodName = match.Groups["methodname"].Value;

        if (!TryGetScriptRoot(out var scriptRoot))
        {
            return false;
        }

        var scriptFile = Path.Combine(scriptRoot, functionMetadata.ScriptFile ?? string.Empty);
        var assemblyPath = Path.GetFullPath(scriptFile);
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        var type = assembly.GetType(typeName);

        if (type is null)
        {
            return false;
        }

        method = type.GetMethod(methodName);
        return method is not null;
    }

    /// <summary>
    /// Attempts to get the script root directory from environment variables.
    /// </summary>
    public static bool TryGetScriptRoot([NotNullWhen(true)] out string? scriptRoot)
    {
        scriptRoot = Environment.GetEnvironmentVariable(FunctionsApplicationDirectoryKey)
                    ?? Environment.GetEnvironmentVariable(FunctionsWorkerDirectoryKey);

        return !string.IsNullOrWhiteSpace(scriptRoot);
    }

    /// <summary>
    /// Ensures the script root environment variable is set.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the script root is not configured.</exception>
    public static void EnsureScriptRoot()
    {
        var scriptRoot = Environment.GetEnvironmentVariable(FunctionsApplicationDirectoryKey)
                        ?? Environment.GetEnvironmentVariable(FunctionsWorkerDirectoryKey);

        if (string.IsNullOrWhiteSpace(scriptRoot))
        {
            throw new InvalidOperationException($"The '{FunctionsApplicationDirectoryKey}' environment variable value is not defined. This is a required environment variable that is automatically set by the Azure Functions runtime.");
        }
    }

    [GeneratedRegex(@"^(?<typename>.*)\.(?<methodname>\S*)$")]
    private static partial Regex GetEntryPointRegex();
}
