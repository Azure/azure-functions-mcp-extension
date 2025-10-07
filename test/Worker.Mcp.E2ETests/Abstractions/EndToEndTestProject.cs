// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests;

namespace Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Abstractions
{
    public class EndToEndTestProject
    {
        public required string ProjectDirectoryPath { get; set; }
        public required string FunctionsWorkerRuntime { get; set; }

        public List<string>? AdditionalCoreToolsArguments { get; set; }

        private IDictionary<string, string>? _launchEnvironmentVariables;

        private void SetLaunchEnvironmentVariables(IDictionary<string, string>? environmentVariables)
        {
            if (environmentVariables == null)
            {
                return;
            }

            _launchEnvironmentVariables = environmentVariables;

            if (!_launchEnvironmentVariables.ContainsKey(Constants.AzureWebJobsStorage))
            {
                _launchEnvironmentVariables[Constants.AzureWebJobsStorage] = "UseDevelopmentStorage=true";
            }

            // User setting the environment variable should win over the property value.
            if (!_launchEnvironmentVariables.ContainsKey(Constants.FunctionsWorkerRuntime))
            {
                _launchEnvironmentVariables[Constants.FunctionsWorkerRuntime] = FunctionsWorkerRuntime;
            }

            Environment.SetEnvironmentVariable(Constants.FunctionsWorkerRuntime, FunctionsWorkerRuntime);

        }

        public IDictionary<string, string> LaunchEnvironmentVariables {
            get => _launchEnvironmentVariables ??= new Dictionary<string, string>
            {
                { Constants.AzureWebJobsStorage, "UseDevelopmentStorage=true" },
                { Constants.FunctionsWorkerRuntime, FunctionsWorkerRuntime }
            };
            set => SetLaunchEnvironmentVariables(value ?? new Dictionary<string, string>());
        }
    }
}
