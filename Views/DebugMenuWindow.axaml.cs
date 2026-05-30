using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MicroPanel.Services;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using MicroPanel.Controls;

namespace MicroPanel.Views
{
    /// <summary>
    /// 调试菜单窗口
    /// </summary>
    public partial class DebugMenuWindow : MyWindow
    {
        private NavigationView? _navigationView;
        private TextBlock? _contentTextBlock;

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
            
            _navigationView = this.FindControl<NavigationView>("NavigationView");
            _contentTextBlock = this.FindControl<TextBlock>("ContentTextBlock");
        }

        /// <summary>
        /// NavigationView 选择改变事件处理
        /// </summary>
        private void NavigationView_SelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
        {
            if (e.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                HandleNavigationItemSelected(tag);
            }
        }

        /// <summary>
        /// 处理导航项选择
        /// </summary>
        private void HandleNavigationItemSelected(string tag)
        {
            switch (tag)
            {
                case "AppInfo":
                    ShowAppInfo();
                    break;
                case "MemoryInfo":
                    ShowMemoryInfo();
                    break;
                case "NetworkTest":
                    TestNetwork();
                    break;
                case "TestCrash":
                    TestCrash();
                    break;
                case "TestNullRef":
                    TestNullReference();
                    break;
                case "TestUiFreeze":
                    TestUiFreeze();
                    break;
                case "ReloadConfig":
                    ReloadConfig();
                    break;
                case "ToggleMode":
                    ToggleMode();
                    break;
                case "ExitDebugMode":
                    ExitDebugMode();
                    break;
            }
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
        private void ShowAppInfo()
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
            if (_contentTextBlock != null)
            {
                _contentTextBlock.Text = info;
            }
        }

        /// <summary>
        /// 显示内存信息
        /// </summary>
        private void ShowMemoryInfo()
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
            if (_contentTextBlock != null)
            {
                _contentTextBlock.Text = info;
            }
        }

        /// <summary>
        /// 测试网络连接
        /// </summary>
        private async void TestNetwork()
        {
            if (_contentTextBlock != null)
            {
                _contentTextBlock.Text = "正在测试网络连接...";
            }

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var response = await client.GetAsync("https://www.baidu.com");
                var result = $"""
                    网络连接测试结果:
                    - 状态: {(response.IsSuccessStatusCode ? "成功" : "失败")}
                    - 状态码: {response.StatusCode}
                    - 响应时间: {DateTime.Now:HH:mm:ss}
                    """;
                DebugModeService.LogDebug(result);
                if (_contentTextBlock != null)
                {
                    _contentTextBlock.Text = result;
                }
            }
            catch (Exception ex)
            {
                var error = $"""
                    网络连接测试失败:
                    - 错误: {ex.Message}
                    - 时间: {DateTime.Now:HH:mm:ss}
                    """;
                DebugModeService.LogDebug(error);
                if (_contentTextBlock != null)
                {
                    _contentTextBlock.Text = error;
                }
            }
        }

        /// <summary>
        /// 测试崩溃 - 立即崩溃（带二次确认）
        /// </summary>
        private async void TestCrash()
        {
            var dialog = new ContentDialog
            {
                Title = "危险操作确认",
                Content = "确定要立即崩溃应用吗？\n\n这将导致应用异常退出，未保存的数据可能会丢失。",
                PrimaryButtonText = "确定崩溃",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                DebugModeService.LogDebug("触发测试崩溃!");
                Environment.FailFast("测试崩溃");
            }
            else
            {
                // 用户取消，恢复默认显示
                if (_contentTextBlock != null)
                {
                    _contentTextBlock.Text = "请从左侧菜单选择调试功能";
                }
            }
        }

        /// <summary>
        /// 测试空引用异常（带二次确认）
        /// </summary>
        private async void TestNullReference()
        {
            var dialog = new ContentDialog
            {
                Title = "危险操作确认",
                Content = "确定要测试空引用异常吗？\n\n这将触发一个异常并被捕获。",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                DebugModeService.LogDebug("触发空引用异常测试");
                try
                {
                    string? nullString = null;
                    _ = nullString!.Length; // 这将抛出 NullReferenceException
                }
                catch (NullReferenceException ex)
                {
                    var message = $"""
                        成功捕获空引用异常:
                        - 消息: {ex.Message}
                        - 时间: {DateTime.Now:HH:mm:ss}
                        """;
                    DebugModeService.LogDebug(message);
                    if (_contentTextBlock != null)
                    {
                        _contentTextBlock.Text = message;
                    }
                }
            }
            else
            {
                // 用户取消，恢复默认显示
                if (_contentTextBlock != null)
                {
                    _contentTextBlock.Text = "请从左侧菜单选择调试功能";
                }
            }
        }

        /// <summary>
        /// 测试 UI 卡死（带二次确认）
        /// </summary>
        private async void TestUiFreeze()
        {
            var dialog = new ContentDialog
            {
                Title = "危险操作确认",
                Content = "确定要测试 UI 卡死吗？\n\n应用界面将冻结 5 秒钟。",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                DebugModeService.LogDebug("触发 UI 卡死测试（5秒）");
                if (_contentTextBlock != null)
                {
                    _contentTextBlock.Text = "UI 卡死测试中...（5秒）";
                }
                
                // 在 UI 线程上阻塞 5 秒
                Thread.Sleep(5000);
                
                DebugModeService.LogDebug("UI 卡死测试结束");
                if (_contentTextBlock != null)
                {
                    _contentTextBlock.Text = "UI 卡死测试完成";
                }
            }
            else
            {
                // 用户取消，恢复默认显示
                if (_contentTextBlock != null)
                {
                    _contentTextBlock.Text = "请从左侧菜单选择调试功能";
                }
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        private void ReloadConfig()
        {
            DebugModeService.LogDebug("重新加载配置");
            if (_contentTextBlock != null)
            {
                _contentTextBlock.Text = "配置已重新加载\n\n时间: " + DateTime.Now.ToString("HH:mm:ss");
            }
        }

        /// <summary>
        /// 切换桌面/正常模式
        /// </summary>
        private async void ToggleMode()
        {
            bool isDesktopMode = DesktopModeManager.Instance.IsDesktopMode;
            string targetMode = isDesktopMode ? "正常模式" : "桌面模式";
            string message = isDesktopMode
                ? "确定要重启到正常模式吗？\n\n这将移除桌面模式参数。"
                : "确定要重启到桌面模式吗？\n\n这将同时保留调试模式。";

            var dialog = new ContentDialog
            {
                Title = $"重启到{targetMode}",
                Content = message,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
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
            else
            {
                // 用户取消，恢复默认显示
                if (_contentTextBlock != null)
                {
                    _contentTextBlock.Text = "请从左侧菜单选择调试功能";
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
        private async void ExitDebugMode()
        {
            DebugModeService.LogDebug("退出调试模式");
            
            var dialog = new ContentDialog
            {
                Title = "退出调试模式",
                Content = "确定要退出调试模式并重启应用吗？",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 移除调试模式参数并重启
                RestartWithoutDebugMode();
            }
            else
            {
                // 用户取消，恢复默认显示
                if (_contentTextBlock != null)
                {
                    _contentTextBlock.Text = "请从左侧菜单选择调试功能";
                }
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
    }
}
