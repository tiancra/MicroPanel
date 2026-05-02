using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MicroPanelAvalonia.Services;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MicroPanelAvalonia.Views
{
    /// <summary>
    /// 调试菜单窗口
    /// </summary>
    public partial class DebugMenuWindow : Window
    {
        public DebugMenuWindow()
        {
            InitializeComponent();

            // 根据桌面模式设置置顶
            if (DesktopModeManager.Instance.IsDesktopMode)
            {
                Topmost = true;
                DebugModeService.LogDebug("调试菜单已设置为置顶（桌面模式）");
            }
            else
            {
                Topmost = false;
                DebugModeService.LogDebug("调试菜单未置顶（非桌面模式）");
            }

            // 订阅窗口关闭事件，阻止关闭
            Closing += (s, e) =>
            {
                e.Cancel = true; // 阻止关闭
                DebugModeService.LogDebug("调试菜单关闭被阻止");
            };

            // 更新模式切换按钮文本
            UpdateToggleModeButtonText();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// 更新模式切换按钮文本
        /// </summary>
        private void UpdateToggleModeButtonText()
        {
            var buttonText = this.FindControl<TextBlock>("ToggleModeButtonText");
            if (buttonText != null)
            {
                if (DesktopModeManager.Instance.IsDesktopMode)
                {
                    buttonText.Text = "重启到正常模式";
                }
                else
                {
                    buttonText.Text = "重启到桌面模式";
                }
            }
        }

        /// <summary>
        /// 显示应用信息
        /// </summary>
        private void OnShowAppInfoClick(object? sender, RoutedEventArgs e)
        {
            var info = $"""
                应用信息:
                - 应用名称: Micro Panel
                - 版本: 2.0.0 Beta
                - 进程ID: {Environment.ProcessId}
                - 工作目录: {Environment.CurrentDirectory}
                - 命令行参数: {string.Join(" ", Environment.GetCommandLineArgs())}
                - 调试模式: {DebugModeService.IsDebugMode}
                - 桌面模式: {DesktopModeManager.Instance.IsDesktopMode}
                """;

            DebugModeService.LogDebug(info);
            ShowInfoDialog("应用信息", info);
        }

        /// <summary>
        /// 显示内存信息
        /// </summary>
        private void OnShowMemoryInfoClick(object? sender, RoutedEventArgs e)
        {
            var process = Process.GetCurrentProcess();
            var info = $"""
                内存信息:
                - 工作集: {process.WorkingSet64 / 1024 / 1024} MB
                - 私有内存: {process.PrivateMemorySize64 / 1024 / 1024} MB
                - 虚拟内存: {process.VirtualMemorySize64 / 1024 / 1024} MB
                - GC 总内存: {GC.GetTotalMemory(false) / 1024 / 1024} MB
                - GC 代数: {GC.MaxGeneration}
                """;

            DebugModeService.LogDebug(info);
            ShowInfoDialog("内存信息", info);
        }

        /// <summary>
        /// 测试网络连接
        /// </summary>
        private async void OnTestNetworkClick(object? sender, RoutedEventArgs e)
        {
            DebugModeService.LogDebug("测试网络连接...");
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var response = await client.GetAsync("https://www.baidu.com");
                var result = $"网络连接测试: {(response.IsSuccessStatusCode ? "成功" : "失败")}\n状态码: {response.StatusCode}";
                DebugModeService.LogDebug(result);
                ShowInfoDialog("网络测试", result);
            }
            catch (Exception ex)
            {
                var error = $"网络连接测试失败: {ex.Message}";
                DebugModeService.LogDebug(error);
                ShowInfoDialog("网络测试", error);
            }
        }

        /// <summary>
        /// 测试崩溃 - 立即崩溃（带二次确认）
        /// </summary>
        private async void OnTestCrashClick(object? sender, RoutedEventArgs e)
        {
            var dialog = new FluentAvalonia.UI.Controls.ContentDialog
            {
                Title = "危险操作确认",
                Content = "确定要立即崩溃应用吗？\n\n这将导致应用异常退出，未保存的数据可能会丢失。",
                PrimaryButtonText = "确定崩溃",
                CloseButtonText = "取消",
                DefaultButton = FluentAvalonia.UI.Controls.ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result == FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
            {
                DebugModeService.LogDebug("触发测试崩溃!");
                Environment.FailFast("测试崩溃");
            }
        }

        /// <summary>
        /// 测试空引用异常（带二次确认）
        /// </summary>
        private async void OnTestNullReferenceClick(object? sender, RoutedEventArgs e)
        {
            var dialog = new FluentAvalonia.UI.Controls.ContentDialog
            {
                Title = "危险操作确认",
                Content = "确定要测试空引用异常吗？\n\n这将触发一个异常并被捕获。",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = FluentAvalonia.UI.Controls.ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result == FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
            {
                DebugModeService.LogDebug("触发空引用异常测试");
                try
                {
                    string? nullString = null;
                    _ = nullString!.Length; // 这将抛出 NullReferenceException
                }
                catch (NullReferenceException ex)
                {
                    DebugModeService.LogDebug($"捕获到空引用异常: {ex.Message}");
                    ShowInfoDialog("异常测试", $"成功捕获空引用异常:\n{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 测试 UI 卡死（带二次确认）
        /// </summary>
        private async void OnTestUiFreezeClick(object? sender, RoutedEventArgs e)
        {
            var dialog = new FluentAvalonia.UI.Controls.ContentDialog
            {
                Title = "危险操作确认",
                Content = "确定要测试 UI 卡死吗？\n\n应用界面将冻结 5 秒钟。",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = FluentAvalonia.UI.Controls.ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result == FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
            {
                DebugModeService.LogDebug("触发 UI 卡死测试（5秒）");
                // 在 UI 线程上阻塞 5 秒
                Thread.Sleep(5000);
                DebugModeService.LogDebug("UI 卡死测试结束");
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        private void OnReloadConfigClick(object? sender, RoutedEventArgs e)
        {
            DebugModeService.LogDebug("重新加载配置");
            // 这里可以实现重新加载配置的逻辑
            ShowInfoDialog("提示", "配置已重新加载");
        }

        /// <summary>
        /// 切换桌面/正常模式
        /// </summary>
        private async void OnToggleModeClick(object? sender, RoutedEventArgs e)
        {
            bool isDesktopMode = DesktopModeManager.Instance.IsDesktopMode;
            string targetMode = isDesktopMode ? "正常模式" : "桌面模式";
            string message = isDesktopMode
                ? "确定要重启到正常模式吗？\n\n这将移除桌面模式参数。"
                : "确定要重启到桌面模式吗？\n\n这将同时保留调试模式。";

            var dialog = new FluentAvalonia.UI.Controls.ContentDialog
            {
                Title = $"重启到{targetMode}",
                Content = message,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = FluentAvalonia.UI.Controls.ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
            {
                if (isDesktopMode)
                {
                    RestartToNormalMode();
                }
                else
                {
                    RestartToDesktopMode();
                }
            }
        }

        /// <summary>
        /// 重启到桌面模式
        /// </summary>
        private void RestartToDesktopMode()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var currentArgs = Environment.GetCommandLineArgs();
                
                // 构建新的启动参数，保留原有参数并添加桌面模式参数
                var newArgs = currentArgs.Skip(1).ToList();
                
                // 确保有调试模式参数
                if (!newArgs.Any(arg => DebugModeService.IsDebugModeArgument(arg)))
                {
                    newArgs.Add("--debugmode");
                }
                
                // 添加桌面模式参数（如果不存在）
                if (!newArgs.Any(arg => arg.Equals("--desktopmode", StringComparison.OrdinalIgnoreCase)))
                {
                    newArgs.Add("--desktopmode");
                }

                var argsString = string.Join(" ", newArgs);
                DebugModeService.LogDebug($"重启参数（桌面模式+调试模式）: {argsString}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = currentProcess.MainModule?.FileName ?? Environment.ProcessPath,
                    Arguments = argsString,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                
                if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    lifetime.Shutdown();
                }
            }
            catch (Exception ex)
            {
                DebugModeService.LogDebug($"重启到桌面模式失败: {ex}");
            }
        }

        /// <summary>
        /// 重启到正常模式（移除桌面模式参数）
        /// </summary>
        private void RestartToNormalMode()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var currentArgs = Environment.GetCommandLineArgs();
                
                // 过滤掉桌面模式参数，保留其他参数（包括调试模式）
                var newArgs = currentArgs.Skip(1)
                    .Where(arg => !arg.Equals("--desktopmode", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var argsString = string.Join(" ", newArgs);
                DebugModeService.LogDebug($"重启参数（正常模式+调试模式）: {argsString}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = currentProcess.MainModule?.FileName ?? Environment.ProcessPath,
                    Arguments = argsString,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                
                if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    lifetime.Shutdown();
                }
            }
            catch (Exception ex)
            {
                DebugModeService.LogDebug($"重启到正常模式失败: {ex}");
            }
        }

        /// <summary>
        /// 退出调试模式
        /// </summary>
        private async void OnExitDebugModeClick(object? sender, RoutedEventArgs e)
        {
            DebugModeService.LogDebug("退出调试模式");
            
            var dialog = new FluentAvalonia.UI.Controls.ContentDialog
            {
                Title = "退出调试模式",
                Content = "确定要退出调试模式并重启应用吗？",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = FluentAvalonia.UI.Controls.ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
            {
                // 移除调试模式参数并重启
                RestartWithoutDebugMode();
            }
        }

        /// <summary>
        /// 不带调试模式重启
        /// </summary>
        private void RestartWithoutDebugMode()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var currentArgs = Environment.GetCommandLineArgs();
                
                // 过滤掉调试模式参数
                var newArgs = currentArgs.Skip(1)
                    .Where(arg => !DebugModeService.IsDebugModeArgument(arg))
                    .ToList();

                var argsString = string.Join(" ", newArgs);
                DebugModeService.LogDebug($"重启参数（无调试模式）: {argsString}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = currentProcess.MainModule?.FileName ?? Environment.ProcessPath,
                    Arguments = argsString,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                
                if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    lifetime.Shutdown();
                }
            }
            catch (Exception ex)
            {
                DebugModeService.LogDebug($"退出调试模式失败: {ex}");
            }
        }

        /// <summary>
        /// 显示信息对话框
        /// </summary>
        private async void ShowInfoDialog(string title, string content)
        {
            var dialog = new FluentAvalonia.UI.Controls.ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "确定"
            };
            await dialog.ShowAsync();
        }
    }
}
