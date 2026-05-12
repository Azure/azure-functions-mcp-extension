// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;
using static Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters.McpInputConversionHelper;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

internal class ToolInvocationPocoConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.TargetType.IsPoco()
            || context.TargetType == typeof(ToolInvocationContext)
            || !context.FunctionContext.TryGetToolInvocationContext(out ToolInvocationContext? toolContext))
        {
            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }

        if (toolContext is null)
        {
            return new ValueTask<ConversionResult>(
                ConversionResult.Failed(new InvalidOperationException($"{nameof(ToolInvocationContext)} was not available or was null in the current FunctionContext.")));
        }

        try
        {
            // Tool arguments arrive as Dictionary<string, object?>, but the shared helper expects
            // Dictionary<string, object>. Project nulls out so the helper can populate the POCO,
            // including any nested complex or array-of-complex properties.
            var arguments = toolContext.Arguments!
                .Where(kvp => kvp.Value is not null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!, StringComparer.OrdinalIgnoreCase);

            var poco = CreatePocoFromDictionary(arguments, context.TargetType);
            return new ValueTask<ConversionResult>(ConversionResult.Success(poco));
        }
        catch (Exception ex)
        {
            return new ValueTask<ConversionResult>(ConversionResult.Failed(ex));
        }
    }
}
