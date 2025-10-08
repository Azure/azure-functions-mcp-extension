// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Abstractions;

namespace Worker.Mcp.E2ETests.Abstractions;

public abstract class CoreToolsProjectBase : IAsyncLifetime
{
    private readonly EndToEndTestProject? _project;

    private readonly ILogger<CoreToolsProjectBase> _logger;

    private Process _funcProcess = new Process();

    private JobObjectRegistry? _jobObjectRegistry;

    private bool _disposed;

    protected CoreToolsProjectBase(EndToEndTestProject project)
    {
        var loggerFactory = new LoggerFactory();
        _logger = loggerFactory.CreateLogger<CoreToolsProjectBase>();
        _project = project;
    }

    public Uri? AppRootEndpoint { get; set; } = new Uri("http://localhost:7071");

    public async Task InitializeAsync()
    {
        if (_project is null)
        {
            throw new InvalidOperationException("Project is not set. Ensure the fixture is initialized with a valid project.");
        }

        KillExistingFuncHosts();

        await StartCoreToolsForProject();
    }

    private static void KillExistingFuncHosts()
    {
        foreach (var func in Process.GetProcessesByName("func"))
        {
            try
            {
                func.Kill();
            }
            catch
            {
                // Best effort
            }
        }
    }

    private async Task StartCoreToolsForProject()
    {
        _funcProcess = new Process();

        var rootDir = Environment.GetEnvironmentVariable("RepoRoot");
        var cliPath = "func";

        string? e2eHostJson = Directory.GetFiles(_project!.ProjectDirectoryPath, "host.json", SearchOption.AllDirectories).FirstOrDefault();

        if (e2eHostJson == null)
        {
            throw new InvalidOperationException($"Could not find a built worker app under '{_project.ProjectDirectoryPath}'");
        }

        var e2eAppPath = Path.GetDirectoryName(e2eHostJson);

        // Set the path for func if we are running on pipeline
        if (!string.IsNullOrEmpty(rootDir))
        {
            cliPath = Path.Combine(rootDir!, "Azure.Functions.Cli", "func");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                cliPath += ".exe";
            }

            if (!File.Exists(cliPath))
            {
                throw new InvalidOperationException($"Could not find '{cliPath}'. Try running '{Path.Combine(rootDir, "setup-e2e-tests.ps1")}' to install it.");
            }

        }

        _funcProcess.StartInfo.UseShellExecute = false;
        _funcProcess.StartInfo.RedirectStandardError = true;
        _funcProcess.StartInfo.RedirectStandardOutput = true;
        _funcProcess.StartInfo.CreateNoWindow = true;
        _funcProcess.StartInfo.WorkingDirectory = e2eAppPath;
        _funcProcess.StartInfo.FileName = cliPath;
        _funcProcess.StartInfo.ArgumentList.Add("start");
        _funcProcess.StartInfo.ArgumentList.Add("--verbose");

        foreach (var arg in _project.AdditionalCoreToolsArguments ?? new List<string>())
        {
            _funcProcess.StartInfo.ArgumentList.Add(arg);
        }

        foreach (var env in _project.LaunchEnvironmentVariables)
        {
            _funcProcess.StartInfo.Environment[env.Key] = env.Value;
        }

        _funcProcess.ErrorDataReceived += (sender, e) => _logger.LogError(e?.Data);
        _funcProcess.OutputDataReceived += (sender, e) => _logger.LogInformation(e?.Data);

        _funcProcess.Start();

        _logger.LogInformation($"Started '{_funcProcess.StartInfo.FileName}'");

        _funcProcess.BeginErrorReadLine();
        _funcProcess.BeginOutputReadLine();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // ensure child processes are cleaned up
            _jobObjectRegistry = new JobObjectRegistry();
            _jobObjectRegistry.Register(_funcProcess);
        }

        // Use the TestUtility method for waiting for the host to be running
        await TestUtility.WaitForFunctionsHostToBeRunningAsync(
            AppRootEndpoint!, 
            _funcProcess, 
            _logger, 
            timeout: 60000, 
            pollingInterval: 2000);
    }

    public Task DisposeAsync()
    {
        if (!_disposed)
        {
            if (_funcProcess != null)
            {
                try
                {
                    _funcProcess.Kill();
                    _funcProcess.Dispose();
                }
                catch
                {
                    // process may not have started
                }
            }

            _jobObjectRegistry?.Dispose();
        }

        _disposed = true;
        return Task.CompletedTask;
    }
}
