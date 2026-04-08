// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

internal static class MimeTypeHelper
{
    /// <summary>
    /// Determines whether the given MIME type represents text content that should be read as UTF-8.
    /// </summary>
    internal static bool IsTextMimeType(string? mimeType)
    {
        if (string.IsNullOrEmpty(mimeType))
        {
            return true;
        }

        // text/* is always text
        if (mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Common text-based application types
        if (mimeType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
            mimeType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
            mimeType.Equals("application/javascript", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Structured syntax suffixes: *+json, *+xml
        if (mimeType.EndsWith("+json", StringComparison.OrdinalIgnoreCase) ||
            mimeType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
