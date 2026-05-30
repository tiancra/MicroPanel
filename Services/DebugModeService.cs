using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using FluentAvalonia.UI.Controls;

namespace MicroPanel.Services
{
    /// <summary>
    /// 调试模式服务 - 管理应用的调试模式状态和功能
    /// </summary>
    public static class DebugModeService
    {
        private const string DebugModeArg = "--debugmode";
        private static bool _isDebugMode = false;
        private static bool _ctrlAltPressed = false;

        /// <summary>
        /// 是否处于调试模式
        /// </summary>
        public static bool IsDebugMode => _isDebugMode;

        /// <summary>
        /// 初始化调试模式服务
        /// </summary>
        public static void Initialize(string[] args)
        {
            _isDebugMode = args?.Any(arg => 
                arg.Equals(DebugModeArg, StringComparison.OrdinalIgnoreCase)) ?? false;

            if (_isDebugMode)
            {
                // 调试模式下启动控制台
                AllocConsole();
                
                // 启用日志重定向
                DebugLogger.Instance.Enable();
                
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [SYSTEM] === 调试模式已启动 ===");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [SYSTEM] 启动参数: {string.Join(" ", args)}");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [SYSTEM] =================================");
                
                // 设置详细日志级别
                EnableDetailedLogging();
            }
        }

        /// <summary>
        /// 检查是否是调试模式启动参数
        /// </summary>
        public static bool IsDebugModeArgument(string arg)
        {
            return arg?.Equals(DebugModeArg, StringComparison.OrdinalIgnoreCase) ?? false;
        }

        /// <summary>
        /// 设置Ctrl+Alt按键状态
        /// </summary>
        public static void SetCtrlAltPressed(bool pressed)
        {
            _ctrlAltPressed = pressed;
            LogDebug($"Ctrl+Alt 状态: {pressed}");
        }

        /// <summary>
        /// 获取Ctrl+Alt按键状态
        /// </summary>
        public static bool IsCtrlAltPressed => _ctrlAltPressed;

        /// <summary>
        /// 显示调试模式确认对话框
        /// </summary>
        public static async Task<bool> ShowDebugModeConfirmDialog(Window parent)
        {
            // 如果已经在调试模式下，显示提示并返回 false
            if (_isDebugMode)
            {
                var infoDialog = new ContentDialog
                {
                    Title = "提示",
                    Content = "应用当前已经在调试模式下运行。\n\n如需退出调试模式，请重启应用。",
                    CloseButtonText = "我知道了",
                    DefaultButton = ContentDialogButton.Close
                };
                await infoDialog.ShowAsync();
                return false;
            }

            var dialog = new ContentDialog
            {
                Title = "进入调试模式",
                Content = "您即将进入调试模式。\n\n" +
                         "调试模式可能会导致应用运行不稳定，仅供专业人员使用。\n\n" +
                         "确定要重启到调试模式吗？",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// 显示调试模式警告弹窗
        /// </summary>
        public static async Task ShowDebugModeWarningDialog(Window parent)
        {
            var dialog = new ContentDialog
            {
                Title = "调试模式警告",
                Content = "应用当前正在调试模式中运行。\n\n" +
                         "这可能会导致应用运行不稳定，仅供专业人员使用。\n\n" +
                         "如需退出调试模式，请重启应用。",
                CloseButtonText = "我知道了",
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }

        /// <summary>
        /// 以调试模式重启应用
        /// </summary>
        public static void RestartInDebugMode()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var currentArgs = Environment.GetCommandLineArgs();
                
                // 构建新的启动参数，保留原有参数并添加调试模式参数
                var newArgs = currentArgs.Skip(1).ToList(); // 跳过程序路径
                
                // 如果已经有调试模式参数，不再添加
                if (!newArgs.Any(arg => IsDebugModeArgument(arg)))
                {
                    newArgs.Add(DebugModeArg);
                }

                var argsString = string.Join(" ", newArgs);
                LogDebug($"重启参数: {argsString}");

                // 启动新进程
                var startInfo = new ProcessStartInfo
                {
                    FileName = currentProcess.MainModule?.FileName ?? Environment.ProcessPath,
                    Arguments = argsString,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                
                // 关闭当前应用
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    lifetime.Shutdown();
                }
            }
            catch (Exception ex)
            {
                LogDebug($"重启到调试模式失败: {ex}");
                throw;
            }
        }

        /// <summary>
        /// 输出调试日志到控制台
        /// </summary>
        public static void LogDebug(string message)
        {
            if (_isDebugMode)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [DEBUG] {message}");
            }
        }

        /// <summary>
        /// 输出详细日志
        /// </summary>
        public static void LogVerbose(string category, string message)
        {
            if (_isDebugMode)
            {
                var stackTrace = new StackTrace(1, true);
                var frame = stackTrace.GetFrame(0);
                var methodName = frame?.GetMethod()?.Name ?? "Unknown";
                var fileName = frame?.GetFileName() ?? "Unknown";
                var lineNumber = frame?.GetFileLineNumber() ?? 0;

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [VERBOSE] [{category}] {message}");
                Console.WriteLine($"    位置: {methodName} in {fileName}:{lineNumber}");
            }
        }

        /// <summary>
        /// 分配控制台窗口（Windows API）
        /// </summary>
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        /// <summary>
        /// 启用详细日志记录
        /// </summary>
        private static void EnableDetailedLogging()
        {
            // 设置环境变量启用详细日志
            Environment.SetEnvironmentVariable("AVALONIA_LOG_LEVEL", "Verbose");
            Environment.SetEnvironmentVariable("MICRO_PANEL_LOG_LEVEL", "Debug");
            
            LogDebug("详细日志记录已启用");
        }
    }
}
