// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

internal sealed class ToolOptionsValidator : IValidateOptions<ToolOptions>
{
    public ValidateOptionsResult Validate(string? name, ToolOptions options)
    {
        if (options.AppOptions is null)
        {
            return ValidateOptionsResult.Success;
        }

        var appOptions = options.AppOptions;

        if (appOptions.View is null)
        {
            return ValidateOptionsResult.Fail(
                $"MCP App tool '{name}' has no view configured. " +
                $"Call WithView() with a McpViewSource or file path.");
        }

        if (appOptions.View.Source is null)
        {
            return ValidateOptionsResult.Fail(
                $"MCP App tool '{name}' has a view with no source configured. " +
                $"Call WithView() with a McpViewSource or file path.");
        }

        if (appOptions.StaticAssetsDirectory is not null
            && string.IsNullOrWhiteSpace(appOptions.StaticAssetsDirectory))
        {
            return ValidateOptionsResult.Fail(
                $"MCP App tool '{name}' has an empty StaticAssetsDirectory. " +
                $"Provide a valid directory path or remove the WithStaticAssets() call.");
        }

        return ValidateOptionsResult.Success;
    }
}
