using System;
using Avalonia.Controls;
using ImmersingLinker.UI.Services;

namespace ImmersingLinker.UI.Views;

public partial class LaunchWindow : Window
{
    public LaunchWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        BackendLaunchService.Instance.StatusChanged += OnStatusChanged;
        await BackendLaunchService.Instance.StartAsync();
    }

    private async void OnStatusChanged(BackendStatus status)
    {
        if (status != BackendStatus.Running)
            return;

        var ok = await BackendLaunchService.Instance.TestConnectionAsync();
        if (ok)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(Close);
        }
    }
}