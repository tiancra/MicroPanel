using Avalonia;
using MicroPanel.Services;
using System;

namespace MicroPanel;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        DesktopModeManager.Instance.Initialize(args);

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
