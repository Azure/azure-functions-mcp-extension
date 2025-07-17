// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit.Abstractions;
using static Microsoft.Azure.Functions.Extensions.Mcp.EndToEnd.Fixtures.McpEndToEndProjects;

namespace Extensions.Mcp.EndToEnd.Fixtures;

public class McpEndToEndProjectFixtures
{
    // These fixtures should be used only as class fixtures. Setting them as collection fixtures creates coupling across test classes.

    public class DefaultProjectFixture(IMessageSink messageSink) : McpEndToEndFixtureBase(messageSink, new InProcNet8Project("TestApp")) { } // This is a lie. It should be in-proc. But that's giving an error about WebJobs.Script loading.

    public class CustomServerProjectFixture(IMessageSink messageSink) : McpEndToEndFixtureBase(messageSink, new DotnetWorkerProject("TestAppIsolated")) { }

}
