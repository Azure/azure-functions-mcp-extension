// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.ProtocolTests;

public abstract class McpE2ETestBase : IAsyncLifetime, IClassFixture<DefaultProjectFixture>
{
    private readonly DefaultProjectFixture _fixture;
    private readonly ITestOutputHelper _testOutputHelper;

    protected McpE2ETestBase(DefaultProjectFixture fixture, ITestOutputHelper testOutputHelper)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _fixture = fixture;
        _testOutputHelper = testOutputHelper;
    }

    protected DefaultProjectFixture Fixture => _fixture;

    protected ITestOutputHelper TestOutputHelper => _testOutputHelper;

    public async ValueTask InitializeAsync()
    {
        await _fixture.InitializeAsync();
    }

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();
}
