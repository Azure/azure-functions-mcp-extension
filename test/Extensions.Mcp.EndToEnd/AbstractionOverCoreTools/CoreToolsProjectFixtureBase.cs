// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Tests.E2ETests;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Xunit.Abstractions;

namespace Extensions.Mcp.EndToEnd.AbstractionOverCoreTools;



// I'm aware that this will break a bunch of tests if not singleton'd, due to the process kill
// We need a way to slice the testing up by project, which would allow us to tests in serial


public abstract class CoreToolsProjectFixtureBase: IAsyncLifetime
{
    private readonly EndToEndTestProject? _project;

    private readonly ILogger<CoreToolsProjectFixtureBase> _logger;

    private readonly IMessageSink _messageSink;

    private readonly AzuriteFixture _azurite;

    private Process _funcProcess = new Process();

    private JobObjectRegistry? _jobObjectRegistry;

    private bool _disposed;

    internal TestLoggerProvider TestLogs { get; private set; }

    protected CoreToolsProjectFixtureBase(IMessageSink messageSink, EndToEndTestProject project)
    {
        _messageSink = messageSink;
        _azurite = new(_messageSink);
        var loggerFactory = new LoggerFactory();
        TestLogs = new TestLoggerProvider(messageSink);
        loggerFactory.AddProvider(TestLogs);
        _logger = loggerFactory.CreateLogger<CoreToolsProjectFixtureBase>();
        _project = project;
    }

    protected Uri? AppRootEndpoint { get; set; } = new Uri("http://localhost:7071"); // make this be more configurable

    public async Task InitializeAsync()
    {
        if (_project is null)
        {
            throw new InvalidOperationException("Project is not set. Ensure the fixture is initialized with a valid project.");
        }

        KillExistingFuncHosts();

        await _azurite.InitializeAsync();

        await StartCoreToolsForProject();
    }

    public async Task DisposeAsync()
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

        await _azurite.DisposeAsync();
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

        string e2eHostJson = Directory.GetFiles(_project.ProjectDirectoryPath, "host.json", SearchOption.AllDirectories).FirstOrDefault();

        if (e2eHostJson == null)
        {
            throw new InvalidOperationException($"Could not find a built worker app under '{_project.ProjectDirectoryPath}'");
        }

        var e2eAppPath = Path.GetDirectoryName(e2eHostJson);

        //var cliPath = Path.Combine(rootDir, "Azure.Functions.Cli", "func");

        //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //{
        //    cliPath += ".exe";
        //}

        //if (!File.Exists(cliPath))
        //{
        //    throw new InvalidOperationException($"Could not find '{cliPath}'. Try running '{Path.Combine(rootDir, "setup-e2e-tests.ps1")}' to install it.");
        //}

        _funcProcess.StartInfo.UseShellExecute = false;
        _funcProcess.StartInfo.RedirectStandardError = true;
        _funcProcess.StartInfo.RedirectStandardOutput = true;
        _funcProcess.StartInfo.CreateNoWindow = true;
        _funcProcess.StartInfo.WorkingDirectory = e2eAppPath;
        _funcProcess.StartInfo.FileName = "func"; // modified for convenience, but we should make this more robust.
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

        var httpClient = new HttpClient();
        _logger.LogInformation("Waiting for host to be running...");
        await TestUtility.RetryAsync(async () =>
        {
            try
            {
                var response = await httpClient.GetAsync(new Uri(AppRootEndpoint, "admin/host/status"));
                var content = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("state", out JsonElement value) &&
                    value.GetString() == "Running")
                {
                    _logger.LogInformation($"  Current state: Running");
                    return true;
                }

                _logger.LogInformation($"  Current state: {value}");
                return false;
            }
            catch
            {
                if (_funcProcess.HasExited)
                {
                    // Something went wrong starting the host - check the logs
                    _logger.LogInformation($"  Current state: process exited - something may have gone wrong.");
                    return false;
                }

                // Can get exceptions before host is running.
                _logger.LogInformation($"  Current state: process starting");
                return false;
            }
        }, userMessageCallback: () => string.Join(System.Environment.NewLine, TestLogs.CoreToolsLogs));
    }
}
