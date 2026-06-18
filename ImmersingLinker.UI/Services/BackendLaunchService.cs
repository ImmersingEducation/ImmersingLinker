using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ImmersingLinker.UI.Services;

public enum BackendStatus
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Error
}

public class BackendLaunchService
{
    public static BackendLaunchService Instance { get; } = new BackendLaunchService();

    private Process? _process;
    private readonly HttpClient _httpClient;
    private Timer? _healthCheckTimer;

    private const string BackendUrl = "http://localhost:5157";

    private static readonly string ServerDll = Path.Combine(
        AppContext.BaseDirectory, "ImmersingLinker.Server.dll");

    private static readonly string ServerProject = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..",
            "ImmersingLinker.Server", "ImmersingLinker.Server.csproj"));

    private static readonly string BaseDir = AppContext.BaseDirectory;

    public BackendStatus Status { get; private set; } = BackendStatus.Stopped;

    public event Action<BackendStatus>? StatusChanged;

    public BackendLaunchService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BackendUrl),
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task StartAsync()
    {
        if (Status == BackendStatus.Running || Status == BackendStatus.Starting)
            return;

        SetStatus(BackendStatus.Starting);

        try
        {
            string fileName, arguments;
            if (File.Exists(ServerDll))
            {
                fileName = "dotnet";
                arguments = $"\"{ServerDll}\" --urls {BackendUrl}";
            }
            else if (File.Exists(ServerProject))
            {
                fileName = "dotnet";
                arguments = $"run --project \"{ServerProject}\" -- --urls {BackendUrl}";
            }
            else
            {
                SetStatus(BackendStatus.Error);
                return;
            }

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = BaseDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            _process.Exited += OnProcessExited;
            _process.Start();

            _healthCheckTimer = new Timer(
                async _ => await CheckHealthAsync(),
                null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
        }
        catch
        {
            SetStatus(BackendStatus.Error);
        }
    }

    public async Task StopAsync()
    {
        if (_process == null || _process.HasExited)
        {
            CleanupProcess();
            SetStatus(BackendStatus.Stopped);
            return;
        }

        SetStatus(BackendStatus.Stopping);

        _healthCheckTimer?.Dispose();
        _healthCheckTimer = null;

        try
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
        }
        catch
        {
        }

        CleanupProcess();
        SetStatus(BackendStatus.Stopped);
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/app/hello");
            if (!response.IsSuccessStatusCode)
                return false;

            var content = await response.Content.ReadAsStringAsync();
            return content == "Hello world!";
        }
        catch
        {
            return false;
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        CleanupProcess();
        if (Status == BackendStatus.Running || Status == BackendStatus.Starting)
        {
            SetStatus(BackendStatus.Stopped);
        }
    }

    private async Task CheckHealthAsync()
    {
        if (Status != BackendStatus.Starting && Status != BackendStatus.Running)
            return;

        var isHealthy = await TestConnectionAsync();
        if (isHealthy && Status == BackendStatus.Starting)
        {
            SetStatus(BackendStatus.Running);
        }
    }

    private void CleanupProcess()
    {
        if (_process != null)
        {
            _process.Exited -= OnProcessExited;
            _process.Dispose();
            _process = null;
        }

        _healthCheckTimer?.Dispose();
        _healthCheckTimer = null;
    }

    private void SetStatus(BackendStatus status)
    {
        Status = status;
        StatusChanged?.Invoke(status);
    }
}
