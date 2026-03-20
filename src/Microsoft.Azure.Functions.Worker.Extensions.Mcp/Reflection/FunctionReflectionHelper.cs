// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;

/// <summary>
/// Provides helper methods for reflection operations on function metadata.
/// </summary>
internal static partial class FunctionReflectionHelper
{
    private const string FunctionsWorkerDirectoryKey = "FUNCTIONS_WORKER_DIRECTORY";
    private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";

    [GeneratedRegex(@"^(?<typename>.*)\.(?<methodname>\S*)$")]
    private static partial Regex GetEntryPointRegex();

    /// <summary>
    /// Attempts to resolve the method from function metadata.
    /// </summary>
    /// <param name="functionMetadata">The function metadata.</param>
    /// <param name="method">The resolved method if successful.</param>
    /// <returns>True if the method was successfully resolved, false otherwise.</returns>
    public static bool TryResolveMethod(IFunctionMetadata functionMetadata, out MethodInfo? method)
    {
        method = null;

        var match = GetEntryPointRegex().Match(functionMetadata.EntryPoint ?? string.Empty);
        if (!match.Success)
        {
            return false;
        }

        var typeName = match.Groups["typename"].Value;
        var methodName = match.Groups["methodname"].Value;

        var scriptRoot = GetScriptRoot();
        if (string.IsNullOrWhiteSpace(scriptRoot))
        {
            return false;
        }

        try
        {
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
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to resolve the method from function metadata, throwing an exception if the script root is not found.
    /// This is used when we expect the environment to be properly configured.
    /// </summary>
    /// <param name="functionMetadata">The function metadata.</param>
    /// <param name="method">The resolved method if successful.</param>
    /// <returns>True if the method was successfully resolved, false otherwise.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the script root environment variable is not set.</exception>
    public static bool TryResolveMethodStrict(IFunctionMetadata functionMetadata, out MethodInfo? method)
    {
        method = null;

        var match = GetEntryPointRegex().Match(functionMetadata.EntryPoint ?? string.Empty);
        if (!match.Success)
        {
            return false;
        }

        var typeName = match.Groups["typename"].Value;
        var methodName = match.Groups["methodname"].Value;

        var scriptRoot = GetScriptRoot();
        if (string.IsNullOrWhiteSpace(scriptRoot))
        {
            throw new InvalidOperationException($"The '{FunctionsApplicationDirectoryKey}' environment variable value is not defined. This is a required environment variable that is automatically set by the Azure Functions runtime.");
        }

        try
        {
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
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the script root directory from environment variables.
    /// </summary>
    /// <returns>The script root directory path, or null if not found.</returns>
    private static string? GetScriptRoot()
    {
        return Environment.GetEnvironmentVariable(FunctionsApplicationDirectoryKey)
               ?? Environment.GetEnvironmentVariable(FunctionsWorkerDirectoryKey);
    }
}
