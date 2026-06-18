using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ImmersingLinker.UI.Services;

namespace ImmersingLinker.UI.ViewModels;

public partial class LaunchWindowViewModel : ViewModelBase
{
    private readonly BackendLaunchService _backendService;

    [ObservableProperty]
    private string _statusText = "正在启动……";

    public LaunchWindowViewModel()
    {
        _backendService = BackendLaunchService.Instance;
    }

    public async Task StartBackendAsync()
    {
        _backendService.StatusChanged += OnStatusChanged;
        await _backendService.StartAsync();
    }

    private async void OnStatusChanged(BackendStatus status)
    {
        if (status == BackendStatus.Running)
        {
            var ok = await _backendService.TestConnectionAsync();
            if (ok)
            {
                CloseRequested?.Invoke();
            }
        }
        else if (status == BackendStatus.Error)
        {
            StatusText = "启动失败";
        }
        else if (status == BackendStatus.Starting)
        {
            StatusText = "正在启动……";
        }
    }

    public event Action? CloseRequested;
}
