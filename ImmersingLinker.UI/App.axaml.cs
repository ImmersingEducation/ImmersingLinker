using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ImmersingLinker.UI.ViewModels;
using ImmersingLinker.UI.Views;

namespace ImmersingLinker.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

            var launchVm = new LaunchWindowViewModel();
            var launchWindow = new LaunchWindow
            {
                DataContext = launchVm
            };

            launchWindow.Opened += async (_, _) =>
            {
                await launchVm.StartBackendAsync();
            };

            launchVm.CloseRequested += () =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    launchWindow.Close();
                });
            };

            desktop.MainWindow = launchWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
