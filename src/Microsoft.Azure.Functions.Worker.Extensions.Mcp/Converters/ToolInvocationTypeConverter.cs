// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Converters
{
    internal class ToolInvocationTypeConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            Type? sourceType = context.Source?.GetType();

            if (sourceType is not null &&
                context.TargetType.IsAssignableFrom(sourceType))
            {
                var conversionResult = ConversionResult.Success(context.Source);
                return new ValueTask<ConversionResult>(conversionResult);
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
