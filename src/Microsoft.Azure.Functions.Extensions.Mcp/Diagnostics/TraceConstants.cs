// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics;

internal sealed class TraceConstants
{
    public const string ExtensionActivitySource = "Azure.Functions.Extensions.Mcp";
    public const string ExtensionActivitySourceVersion = "1.0.0.0";

    public const string ExceptionEventNameAttribute = "exception";
    public const string ExceptionTypeAttribute = "exception.type";
    public const string ExceptionMessageAttribute = "exception.message";
    public const string ExceptionStacktraceAttribute = "exception.stacktrace";
}
