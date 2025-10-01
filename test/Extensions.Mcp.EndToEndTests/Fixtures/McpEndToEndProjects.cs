// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.EndToEndTests.AbstractionOverCoreTools;

namespace Microsoft.Azure.Functions.Extensions.Mcp.EndToEndTests.Fixtures;

internal class McpEndToEndProjects
{
    private static string RepoRoot =>
        Environment.GetEnvironmentVariable("RepoRoot")
        ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"../../../.."));

    private static string BuildConfiguration = Environment.GetEnvironmentVariable("configuration") ?? "debug";

    private static string RelativePathForTestName(string projectName) => Path.Combine(RepoRoot, "out", "bin", projectName, BuildConfiguration);

    internal class DotnetWorkerProject : EndToEndTestProject
    {
        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public DotnetWorkerProject(string projectName) : base()
        {
            ProjectDirectoryPath = Path.Combine(RepoRoot, RelativePathForTestName(projectName));
            FunctionsWorkerRuntime = "dotnet-isolated";
        }
    }

    internal class InProcNet8Project : EndToEndTestProject
    {
        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public InProcNet8Project(string projectName) : base()
        {
            ProjectDirectoryPath = Path.Combine(RepoRoot, RelativePathForTestName(projectName));
            FunctionsWorkerRuntime = "dotnet";
            AdditionalCoreToolsArguments = ["--runtime", "inproc8"];
            LaunchEnvironmentVariables = new Dictionary<string, string> { { "FUNCTIONS_INPROC_NET8_ENABLED", "1" } }; // This needs to be in the local.settings.json. Cannot rely on env var for now.
        }
    }
}
