// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.DependencyInjection.Converters;

internal class PocoConverter : IInputConverter
{
    private readonly ObjectSerializer _serializer;

    public PocoConverter(IOptions<WorkerOptions> workerOptions)
    {
        ArgumentNullException.ThrowIfNull(workerOptions);

        if (workerOptions.Value.Serializer is null)
        {
            throw new ArgumentException(nameof(workerOptions.Value.Serializer), "Serializer cannot be null.");
        }

        _serializer = workerOptions.Value.Serializer;
    }

    ValueTask<ConversionResult> IInputConverter.ConvertAsync(ConverterContext context) => ConvertAsync(context, CancellationToken.None);

    public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            if (context.TargetType == typeof(string)
                || context.TargetType == typeof(ToolInvocationContext)
                || context.Source is not string json)
            {
                return ConversionResult.Unhandled();
            }

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("arguments", out var argumentsElement))
            {
                return ConversionResult.Unhandled();
            }

            var argumentsJson = argumentsElement.GetRawText();
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
        using var stream = ToStream(json);
        var result = await _serializer.DeserializeAsync(stream, targetType, cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException($"Unable to convert to {targetType}.");
        }

        return result;
    }

    private Stream ToStream(string json)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }
}
