// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using static Microsoft.Azure.Functions.Extensions.Mcp.E2ETests.Fixtures.McpEndToEndProjects;

namespace Extensions.Mcp.E2ETests.Fixtures;

public class McpEndToEndProjectFixtures
{
    // These fixtures should be used only as class fixtures. Setting them as collection fixtures creates coupling across test classes.

    public class InProcProjectFixture() : McpEndToEndFixtureBase(new InProcNet8Project("TestApp")) { }

    public class DefaultProjectFixture() : McpEndToEndFixtureBase(new DotnetWorkerProject("TestAppIsolated")) { }

}
