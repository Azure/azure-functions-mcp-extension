// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Worker.Mcp.E2ETests.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Logging;

public class XUnitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public XUnitLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
        => new XUnitLogger(_output, categoryName);

    public void Dispose() { }
}

