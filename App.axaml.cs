using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using FluentAvalonia.UI;
using System;

namespace MicroPanelAvalonia;

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
            desktop.MainWindow = new MainWindow();
            SetApplicationIcon(desktop.MainWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetApplicationIcon(Window window)
    {
        try
        {
            var uri = new Uri("avares://MicroPanelAvalonia/Assets/logo.png");
            var stream = AssetLoader.Open(uri);
            window.Icon = new WindowIcon(stream);
        }
        catch
        {
            // 图标加载失败时静默处理
        }
    }

    /// <summary>
    /// 为指定窗口设置应用图标
    /// </summary>
    public static void SetWindowIcon(Window window)
    {
        try
        {
            var uri = new Uri("avares://MicroPanelAvalonia/Assets/logo.png");
            var stream = AssetLoader.Open(uri);
            window.Icon = new WindowIcon(stream);
        }
        catch
        {
            // 图标加载失败时静默处理
        }
    }
}
