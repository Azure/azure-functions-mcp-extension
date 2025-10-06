// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Extensions.Mcp.E2ETests.AbstractionOverCoreTools;

namespace Microsoft.Azure.Functions.Extensions.Mcp.E2ETests.Fixtures;

internal class McpEndToEndProjects
{
    private static string RepoRoot =>
        Environment.GetEnvironmentVariable("RepoRoot")
        ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, Path.Combine("..", "..", "..", "..")));

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
        }
    }
}
