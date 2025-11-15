// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Logging;

public class XUnitLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    private readonly string _category;

    public XUnitLogger(ITestOutputHelper output, string category)
    {
        _output = output;
        _category = category;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId,
                            TState state, Exception? exception,
                            Func<TState, Exception?, string> formatter)
    {
        _output.WriteLine($"{logLevel}: {_category}: {formatter(state, exception)}");
        if (exception != null)
            _output.WriteLine(exception.ToString());
    }
}

