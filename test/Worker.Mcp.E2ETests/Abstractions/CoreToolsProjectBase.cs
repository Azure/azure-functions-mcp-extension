// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Mcp.E2ETests.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Worker.Mcp.E2ETests.Abstractions;

public abstract class CoreToolsProjectBase : IAsyncLifetime
{
    private readonly EndToEndTestProject? _project;

    private Process _funcProcess = new Process();

    private JobObjectRegistry? _jobObjectRegistry;

    private bool _disposed;

    // Buffer for capturing all process output (stdout + stderr)
    private readonly StringBuilder _capturedOutput = new();
    private readonly Lock _outputLock = new();

    protected CoreToolsProjectBase(EndToEndTestProject project)
    {
        _project = project;
    }

    internal Uri? AppRootEndpoint { get; set; } = new Uri("http://localhost:7071");

    public async virtual ValueTask InitializeAsync()
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

    public bool IsFaulted { get; private set; }

    public void LogErrorDetails()
    {
        lock (_outputLock)
        {
            var outputSnapshot = _capturedOutput.ToString();
            var testContext = TestContext.Current;
            if (testContext?.Test != null && testContext.PipelineStage == TestPipelineStage.TestExecution && testContext.TestOutputHelper != null)
            {
                testContext.TestOutputHelper.WriteLine("==== FUNCTIONS HOST OUTPUT (captured) ====");
                testContext.TestOutputHelper.WriteLine(outputSnapshot);
                testContext.AddAttachment("func-host-output.txt", outputSnapshot, true);
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

        _funcProcess.ErrorDataReceived += (sender, e) => Log(e?.Data ?? string.Empty);
        _funcProcess.OutputDataReceived += (sender, e) => Log(e?.Data ?? string.Empty);

        Log(e2eAppPath!);

        _funcProcess.Start();

        Log($"Started '{_funcProcess.StartInfo.FileName}'");

        _funcProcess.BeginErrorReadLine();
        _funcProcess.BeginOutputReadLine();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // ensure child processes are cleaned up
            _jobObjectRegistry = new JobObjectRegistry();
            _jobObjectRegistry.Register(_funcProcess);
        }

        // Avoid using logger tied to TestOutputHelper during initialization since there may be no active test
        ILogger? logger = null; // can be enhanced later to a buffering logger if needed

        try
        {
            // Use the TestUtility method for waiting for the host to be running
            await TestUtility.WaitForFunctionsHostToBeRunningAsync(
                AppRootEndpoint!,
                _funcProcess,
                logger,
                timeout: 60000,
                pollingInterval: 2000);
        }
        catch (Exception ex)
        {
            Log($"EXCEPTION DETAILS: Exception while starting Functions Host: {ex}");
            IsFaulted = true;
        }
    }

    private void Log(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return;
        }

        lock (_outputLock)
        {
            _capturedOutput.AppendLine(data);
        }
    }

    public virtual ValueTask DisposeAsync()
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

        return ValueTask.CompletedTask;
    }
}
