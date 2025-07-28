// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters;

internal class PocoConverter : IInputConverter
{
    ValueTask<ConversionResult> IInputConverter.ConvertAsync(ConverterContext context) => ConvertAsync(context, CancellationToken.None);

    public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TargetType == typeof(string) || context.TargetType == typeof(ToolInvocationContext))
        {
            return ConversionResult.Unhandled();
        }

        try
        {
            context.FunctionContext.TryGetToolInvocationContext(out ToolInvocationContext? toolContext);
            var argumentsJson = JsonSerializer.Serialize(toolContext?.Arguments);
            var result = await DeserializeToTargetType(context.TargetType, argumentsJson, cancellationToken);

            return ConversionResult.Success(result);
        }
        catch (Exception ex)
        {
            return ConversionResult.Failed(ex);
        }
    }

    private async Task<object> DeserializeToTargetType(Type targetType, string json, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var result = await JsonSerializer.DeserializeAsync(stream, targetType, cancellationToken: cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException($"Unable to convert to {targetType}.");
        }

        return result;
    }
}
