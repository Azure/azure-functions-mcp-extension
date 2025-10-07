// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using static Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures.McpEndToEndProjectSetup;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Fixtures;

public class DefaultProjectFixture() : McpEndToEndFixtureBase(new DotnetWorkerProject("TestAppIsolated"))
{

}
