// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Middleware;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal class FunctionsMcpResourceResultMiddleware : IFunctionsWorkerMiddleware
{
    private readonly Func<FunctionContext, object?> _getResult;
    private readonly Action<FunctionContext, object?> _setResult;

    public FunctionsMcpResourceResultMiddleware()
    {
        _getResult = static ctx => ctx.GetInvocationResult()?.Value;
        _setResult = static (ctx, value) =>
        {
            var result = ctx.GetInvocationResult();
            if (result is not null)
            {
                result.Value = value;
            }
        };
    }

    // Test constructor
    internal FunctionsMcpResourceResultMiddleware(
        Func<FunctionContext, object?> getResult,
        Action<FunctionContext, object?> setResult)
    {
        _getResult = getResult;
        _setResult = setResult;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);

        await next(context);

        if (!IsMcpResourceInvocation(context))
        {
            return;
        }

        if (_getResult(context) is not FileResourceContents fileResult)
        {
            return;
        }

        string resolvedPath = ResolvePath(fileResult.Path);
        string? mimeType = GetMimeTypeFromContext(context);

        _setResult(context, await ReadFileAsync(resolvedPath, mimeType));
    }

    internal static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Resource file path cannot be null or empty.", nameof(path));
        }

        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(AppContext.BaseDirectory, path);
    }

    private static async Task<object> ReadFileAsync(string resolvedPath, string? mimeType)
    {
        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException($"Resource file not found: '{resolvedPath}'");
        }

        try
        {
            if (MimeTypeHelper.IsTextMimeType(mimeType))
            {
                return await File.ReadAllTextAsync(resolvedPath);
            }
            else
            {
                return await File.ReadAllBytesAsync(resolvedPath);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Cannot access resource file: '{resolvedPath}'", ex);
        }
    }

    private static string? GetMimeTypeFromContext(FunctionContext context)
    {
        if (context.BindingContext.BindingData.TryGetValue("mimeType", out var mimeTypeValue) ||
            context.BindingContext.BindingData.TryGetValue("MimeType", out mimeTypeValue))
        {
            return mimeTypeValue?.ToString();
        }

        return null;
    }

    private static bool IsMcpResourceInvocation(FunctionContext context)
    {
        return context.Items.ContainsKey(Constants.ResourceInvocationContextKey);
    }
}
