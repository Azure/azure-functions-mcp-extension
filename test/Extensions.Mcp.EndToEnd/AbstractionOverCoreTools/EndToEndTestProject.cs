// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Extensions.Mcp.EndToEnd.AbstractionOverCoreTools
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

            if (!_launchEnvironmentVariables.ContainsKey("AzureWebJobsStorage"))
            {
                _launchEnvironmentVariables["AzureWebJobsStorage"] = "UseDevelopmentStorage=true";
            }

            // User setting the environment variable should win over the property value.
            if (!_launchEnvironmentVariables.ContainsKey("FUNCTIONS_WORKER_RUNTIME"))
            {
                _launchEnvironmentVariables["FUNCTIONS_WORKER_RUNTIME"] = FunctionsWorkerRuntime;
            }

        }

        public IDictionary<string, string> LaunchEnvironmentVariables {
            get => _launchEnvironmentVariables ??= new Dictionary<string, string>
            {
                { "AzureWebJobsStorage", "UseDevelopmentStorage=true" },
                { "FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime }
            };
            set => SetLaunchEnvironmentVariables(value ?? new Dictionary<string, string>());
        }
    }
}
