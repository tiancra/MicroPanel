using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace MicroPanelAvalonia.Services
{
    /// <summary>
    /// 桌面模式管理器 - 管理全屏和分屏窗口布局
    /// </summary>
    public class DesktopModeManager
    {
        private static DesktopModeManager? _instance;
        public static DesktopModeManager Instance => _instance ??= new DesktopModeManager();

        private bool _isDesktopMode = false;
        private Window? _mainWindow;
        private Window? _logWindow;
        private bool _isLogWindowOpen = false;
        private double _splitRatio = 0.5; // 分屏比例，默认 50%
        private const int ResizeHandleWidth = 8; // 调整区域宽度
        private bool _isResizing = false;

        // Windows API 用于隐藏/显示任务栏
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        /// <summary>
        /// 是否启用桌面模式
        /// </summary>
        public bool IsDesktopMode => _isDesktopMode;

        /// <summary>
        /// 日志窗口是否打开
        /// </summary>
        public bool IsLogWindowOpen => _isLogWindowOpen;

        /// <summary>
        /// 当前分屏比例
        /// </summary>
        public double SplitRatio => _splitRatio;

        /// <summary>
        /// 初始化桌面模式
        /// </summary>
        public void Initialize(string[] args)
        {
            _isDesktopMode = args.Contains("--desktopmode");
        }

        /// <summary>
        /// 注册主窗口
        /// </summary>
        public void RegisterMainWindow(Window window)
        {
            _mainWindow = window;

            if (_isDesktopMode)
            {
                // 设置窗口样式为无边框
                window.SystemDecorations = SystemDecorations.None;

                // 移除最小宽度限制
                window.MinWidth = 100;
                window.MinHeight = 100;

                // 窗口加载完成后设置为全屏
                window.Loaded += (s, e) =>
                {
                    HideTaskbar();
                    SetFullScreen(window);

                    // 启用拖动关闭功能
                    var dragCloseService = new WindowDragCloseService();
                    dragCloseService.EnableForWindow(window);
                };

                // 窗口关闭时恢复任务栏
                window.Closed += (s, e) =>
                {
                    ShowTaskbar();
                };
            }
        }

        /// <summary>
        /// 注册日志窗口
        /// </summary>
        public void RegisterLogWindow(Window window)
        {
            _logWindow = window;
            _isLogWindowOpen = true;

            if (_isDesktopMode)
            {
                // 设置窗口样式为无边框
                window.SystemDecorations = SystemDecorations.None;

                // 移除最小宽度限制
                window.MinWidth = 100;
                window.MinHeight = 100;

                // 窗口加载完成后设置分屏布局
                window.Loaded += (s, e) =>
                {
                    ArrangeSplitScreen();
                    SetupResizeHandle(window);

                    // 启用拖动关闭功能
                    var dragCloseService = new WindowDragCloseService();
                    dragCloseService.EnableForWindow(window);
                };

                // 监听窗口关闭
                window.Closed += (s, e) =>
                {
                    CleanupLogWindow();
                };
            }
        }

        /// <summary>
        /// 清理日志窗口状态
        /// </summary>
        private void CleanupLogWindow()
        {
            // 重置鼠标光标
            if (_mainWindow != null)
            {
                _mainWindow.Cursor = Cursor.Default;
            }

            _logWindow = null;
            _isLogWindowOpen = false;
            _isResizing = false;

            if (_isDesktopMode)
            {
                RestoreMainWindowFullScreen();
            }
        }

        /// <summary>
        /// 隐藏 Windows 任务栏
        /// </summary>
        private void HideTaskbar()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    // 隐藏任务栏窗口
                    var taskbarHandle = FindWindow("Shell_TrayWnd", null);
                    if (taskbarHandle != IntPtr.Zero)
                    {
                        ShowWindow(taskbarHandle, SW_HIDE);
                    }

                    // 隐藏开始按钮
                    var startHandle = FindWindow("Button", "开始");
                    if (startHandle == IntPtr.Zero)
                    {
                        startHandle = FindWindowEx(IntPtr.Zero, IntPtr.Zero, (IntPtr)0xC017, null);
                    }
                    if (startHandle != IntPtr.Zero)
                    {
                        ShowWindow(startHandle, SW_HIDE);
                    }

                    System.Diagnostics.Debug.WriteLine("任务栏已隐藏");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"隐藏任务栏失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 显示 Windows 任务栏
        /// </summary>
        private void ShowTaskbar()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    // 显示任务栏窗口
                    var taskbarHandle = FindWindow("Shell_TrayWnd", null);
                    if (taskbarHandle != IntPtr.Zero)
                    {
                        ShowWindow(taskbarHandle, SW_SHOW);
                    }

                    // 显示开始按钮
                    var startHandle = FindWindow("Button", "开始");
                    if (startHandle == IntPtr.Zero)
                    {
                        startHandle = FindWindowEx(IntPtr.Zero, IntPtr.Zero, (IntPtr)0xC017, null);
                    }
                    if (startHandle != IntPtr.Zero)
                    {
                        ShowWindow(startHandle, SW_SHOW);
                    }

                    System.Diagnostics.Debug.WriteLine("任务栏已显示");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"显示任务栏失败: {ex.Message}");
                }
            }
        }

        // Windows API 导入
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, IntPtr lpszClass, string? lpszWindow);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// 设置调整手柄（用于拖动调整分屏比例）
        /// </summary>
        private void SetupResizeHandle(Window window)
        {
            window.PointerMoved += OnPointerMoved;
            window.PointerPressed += OnPointerPressed;
            window.PointerReleased += OnPointerReleased;
        }

        /// <summary>
        /// 指针移动事件 - 处理调整光标和拖动
        /// </summary>
        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isDesktopMode || _mainWindow == null) return;

            var window = sender as Window;
            if (window == null) return;

            var position = e.GetPosition(window);
            var screenPosition = window.PointToScreen(position);

            // 检查是否在调整区域（两个窗口的交界处）
            bool isInResizeArea = IsInResizeArea(screenPosition);

            if (_isResizing && _logWindow != null)
            {
                // 正在拖动调整
                var screen = window.Screens?.Primary;
                if (screen != null)
                {
                    var bounds = screen.Bounds;
                    var newRatio = (double)(screenPosition.X - bounds.X) / bounds.Width;
                    // 限制比例范围 20% - 80%
                    newRatio = Math.Max(0.2, Math.Min(0.8, newRatio));

                    if (Math.Abs(newRatio - _splitRatio) > 0.01)
                    {
                        _splitRatio = newRatio;
                        ArrangeSplitScreen();
                    }
                }
            }
            else
            {
                // 更新光标
                if (isInResizeArea && _logWindow != null)
                {
                    window.Cursor = new Cursor(StandardCursorType.SizeWestEast);
                }
                else
                {
                    window.Cursor = Cursor.Default;
                }
            }
        }

        /// <summary>
        /// 指针按下事件
        /// </summary>
        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!_isDesktopMode || _mainWindow == null || _logWindow == null) return;

            var window = sender as Window;
            if (window == null) return;

            var position = e.GetPosition(window);
            var screenPosition = window.PointToScreen(position);

            if (IsInResizeArea(screenPosition))
            {
                _isResizing = true;
                e.Handled = true;
            }
        }

        /// <summary>
        /// 指针释放事件
        /// </summary>
        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                e.Handled = true;
            }
        }

        /// <summary>
        /// 检查屏幕位置是否在调整区域
        /// </summary>
        private bool IsInResizeArea(PixelPoint screenPoint)
        {
            if (_mainWindow == null) return false;

            var mainScreen = _mainWindow.Screens?.Primary;
            if (mainScreen == null) return false;

            var bounds = mainScreen.Bounds;
            var splitX = bounds.X + (int)(bounds.Width * _splitRatio);

            // 检查是否在分界线的 ResizeHandleWidth 范围内
            return Math.Abs(screenPoint.X - splitX) <= ResizeHandleWidth;
        }

        /// <summary>
        /// 设置窗口全屏（覆盖任务栏）
        /// </summary>
        private void SetFullScreen(Window window)
        {
            try
            {
                var screen = window.Screens?.Primary;
                if (screen != null)
                {
                    var bounds = screen.Bounds;
                    window.WindowState = WindowState.Normal;
                    window.Position = new PixelPoint(bounds.X, bounds.Y);
                    window.Width = bounds.Width;
                    window.Height = bounds.Height;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置全屏失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置窗口占据屏幕左半边（根据分屏比例）
        /// </summary>
        private void SetLeftWindow(Window window)
        {
            try
            {
                var screen = window.Screens?.Primary;
                if (screen != null)
                {
                    var bounds = screen.Bounds;
                    var width = (int)(bounds.Width * _splitRatio);
                    window.WindowState = WindowState.Normal;
                    window.Position = new PixelPoint(bounds.X, bounds.Y);
                    window.Width = width;
                    window.Height = bounds.Height;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置左窗口失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置窗口占据屏幕右半边（根据分屏比例）
        /// </summary>
        private void SetRightWindow(Window window)
        {
            try
            {
                var screen = window.Screens?.Primary;
                if (screen != null)
                {
                    var bounds = screen.Bounds;
                    var leftWidth = (int)(bounds.Width * _splitRatio);
                    var width = bounds.Width - leftWidth;
                    window.WindowState = WindowState.Normal;
                    window.Position = new PixelPoint(bounds.X + leftWidth, bounds.Y);
                    window.Width = width;
                    window.Height = bounds.Height;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置右窗口失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 安排分屏布局
        /// </summary>
        private void ArrangeSplitScreen()
        {
            if (_mainWindow == null || _logWindow == null) return;

            SetLeftWindow(_mainWindow);
            SetRightWindow(_logWindow);
        }

        /// <summary>
        /// 恢复主窗口全屏
        /// </summary>
        private void RestoreMainWindowFullScreen()
        {
            if (_mainWindow == null) return;
            SetFullScreen(_mainWindow);
        }

        /// <summary>
        /// 关闭日志窗口时调用
        /// </summary>
        public void OnLogWindowClosed()
        {
            CleanupLogWindow();
        }
    }
}
