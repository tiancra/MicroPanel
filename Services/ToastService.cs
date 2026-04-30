using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using MicroPanelAvalonia.Views.Windows;
using System;
using System.Threading.Tasks;

namespace MicroPanelAvalonia.Services
{
    /// <summary>
    /// Toast 通知服务
    /// 桌面模式：使用独立窗口，覆盖在屏幕顶部
    /// 非桌面模式：使用内嵌控件，显示在窗口顶部
    /// </summary>
    public class ToastService
    {
        private static ToastService? _instance;
        public static ToastService Instance => _instance ??= new ToastService();

        private Window? _hostWindow;
        private ToastWindow? _toastWindow;
        private Border? _embeddedToast;
        private TextBlock? _embeddedToastText;
        private bool _isShowing = false;

        /// <summary>
        /// Toast 类型
        /// </summary>
        public enum ToastType
        {
            Info,    // 绿色
            Warning, // 黄色
            Error    // 红色
        }

        /// <summary>
        /// 初始化 Toast 服务
        /// </summary>
        public void Initialize(Window hostWindow)
        {
            _hostWindow = hostWindow;
        }

        /// <summary>
        /// 显示 Toast 通知
        /// </summary>
        public void Show(string message, ToastType type = ToastType.Info, int durationMs = 3000)
        {
            if (_hostWindow == null) return;

            // 检查是否桌面模式
            bool isDesktopMode = DesktopModeManager.Instance.IsDesktopMode;

            if (isDesktopMode)
            {
                ShowWindowToast(message, type, durationMs);
            }
            else
            {
                ShowEmbeddedToast(message, type, durationMs);
            }
        }

        /// <summary>
        /// 桌面模式：使用独立窗口显示 Toast
        /// </summary>
        private void ShowWindowToast(string message, ToastType type, int durationMs)
        {
            // 如果正在显示，先关闭当前的
            if (_isShowing && _toastWindow != null)
            {
                _toastWindow.Close();
                _toastWindow = null;
            }

            _isShowing = true;

            // 计算位置和大小
            var (position, width, targetY) = CalculateWindowToastPosition();

            // 创建 Toast 窗口
            _toastWindow = new ToastWindow();

            // 设置 Toast 内容和样式
            var (background, foreground) = GetToastStyle(type);
            _toastWindow.SetToast(message, background, foreground);

            // 设置位置和大小
            _toastWindow.Position = position;
            _toastWindow.Width = width;
            _toastWindow.SetTargetY(targetY);

            // 显示 Toast 窗口（作为子窗口）
            _toastWindow.Show(_hostWindow!);

            // 执行显示动画
            _toastWindow.AnimateShow();

            // 延迟后自动隐藏
            _ = AutoHideWindowToast(durationMs);
        }

        /// <summary>
        /// 非桌面模式：使用内嵌控件显示 Toast
        /// </summary>
        private void ShowEmbeddedToast(string message, ToastType type, int durationMs)
        {
            if (_hostWindow == null) return;

            // 如果正在显示，先隐藏当前的
            if (_isShowing && _embeddedToast != null)
            {
                HideEmbeddedToast();
            }

            _isShowing = true;

            // 确保内嵌 Toast 容器已创建
            EnsureEmbeddedToastCreated();

            if (_embeddedToast == null || _embeddedToastText == null) return;

            // 设置样式
            var (background, foreground) = GetToastStyle(type);
            _embeddedToast.Background = background;
            _embeddedToastText.Text = message;
            _embeddedToastText.Foreground = foreground;

            // 显示 Toast
            _embeddedToast.IsVisible = true;
            _embeddedToast.Opacity = 1;
            _embeddedToast.Margin = new Thickness(0, -50, 0, 0); // 初始在窗口外

            // 执行显示动画
            AnimateEmbeddedToastShow();

            // 延迟后自动隐藏
            _ = AutoHideEmbeddedToast(durationMs);
        }

        /// <summary>
        /// 确保内嵌 Toast 容器已创建
        /// </summary>
        private void EnsureEmbeddedToastCreated()
        {
            if (_embeddedToast != null) return;

            _embeddedToast = new Border
            {
                Height = 50,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                Margin = new Thickness(0, -50, 0, 0),
                IsVisible = false,
                ZIndex = 9999,
                CornerRadius = new CornerRadius(0, 0, 8, 8),
                BoxShadow = new BoxShadows(new BoxShadow
                {
                    OffsetX = 0,
                    OffsetY = 4,
                    Blur = 16,
                    Color = new Color(128, 0, 0, 0)
                })
            };

            _embeddedToastText = new TextBlock
            {
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20, 0, 20, 0)
            };

            _embeddedToast.Child = _embeddedToastText;

            // 添加到窗口内容
            if (_hostWindow?.Content is Panel panel)
            {
                panel.Children.Add(_embeddedToast);
            }
            else if (_hostWindow?.Content is Control control)
            {
                var grid = new Grid();
                grid.Children.Add(control);
                grid.Children.Add(_embeddedToast);
                _hostWindow.Content = grid;
            }
        }

        /// <summary>
        /// 内嵌 Toast 显示动画
        /// </summary>
        private void AnimateEmbeddedToastShow()
        {
            if (_embeddedToast == null) return;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            var duration = 300;
            var startTime = DateTime.Now;

            timer.Tick += (s, e) =>
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var progress = elapsed / duration;

                if (progress >= 1.0)
                {
                    timer.Stop();
                    _embeddedToast.Margin = new Thickness(0, 0, 0, 0);
                    return;
                }

                // Ease Out Cubic
                var easeProgress = 1 - Math.Pow(1 - progress, 3);
                var marginTop = -50 + (50 * easeProgress);
                _embeddedToast.Margin = new Thickness(0, marginTop, 0, 0);
            };

            timer.Start();
        }

        /// <summary>
        /// 内嵌 Toast 隐藏动画
        /// </summary>
        private void AnimateEmbeddedToastHide()
        {
            if (_embeddedToast == null) return;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            var duration = 250;
            var startTime = DateTime.Now;

            timer.Tick += (s, e) =>
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var progress = elapsed / duration;

                if (progress >= 1.0)
                {
                    timer.Stop();
                    _embeddedToast.IsVisible = false;
                    _isShowing = false;
                    return;
                }

                // Ease Out Cubic
                var easeProgress = 1 - Math.Pow(1 - progress, 3);
                var marginTop = 0 - (50 * easeProgress);
                _embeddedToast.Margin = new Thickness(0, marginTop, 0, 0);
            };

            timer.Start();
        }

        /// <summary>
        /// 自动隐藏内嵌 Toast
        /// </summary>
        private async Task AutoHideEmbeddedToast(int durationMs)
        {
            await Task.Delay(durationMs);
            AnimateEmbeddedToastHide();
        }

        /// <summary>
        /// 隐藏内嵌 Toast
        /// </summary>
        private void HideEmbeddedToast()
        {
            if (_embeddedToast == null || !_isShowing) return;
            AnimateEmbeddedToastHide();
        }

        /// <summary>
        /// 计算桌面模式下 Toast 窗口位置和大小
        /// </summary>
        private (PixelPoint position, int width, int targetY) CalculateWindowToastPosition()
        {
            if (_hostWindow == null) return (new PixelPoint(0, -50), 400, 0);

            var hostScreen = _hostWindow.Screens?.Primary;
            if (hostScreen == null) return (new PixelPoint(0, -50), 400, 0);

            var bounds = hostScreen.Bounds;
            var toastX = bounds.X;
            var targetY = bounds.Y; // 目标位置：屏幕顶部
            var toastY = targetY - 50; // 初始在屏幕外，动画会滑入
            var width = bounds.Width;
            return (new PixelPoint(toastX, toastY), width, targetY);
        }

        /// <summary>
        /// 获取 Toast 样式
        /// </summary>
        private (SolidColorBrush background, IBrush foreground) GetToastStyle(ToastType type)
        {
            return type switch
            {
                ToastType.Info => (new SolidColorBrush(Color.Parse("#4CAF50")), Brushes.White),
                ToastType.Warning => (new SolidColorBrush(Color.Parse("#FFC107")), Brushes.Black),
                ToastType.Error => (new SolidColorBrush(Color.Parse("#F44336")), Brushes.White),
                _ => (new SolidColorBrush(Color.Parse("#4CAF50")), Brushes.White)
            };
        }

        /// <summary>
        /// 显示成功 Toast（保存成功等）
        /// </summary>
        public void ShowSuccess(string message = "保存成功")
        {
            Show(message, ToastType.Info);
        }

        /// <summary>
        /// 显示警告 Toast
        /// </summary>
        public void ShowWarning(string message)
        {
            Show(message, ToastType.Warning);
        }

        /// <summary>
        /// 显示错误 Toast
        /// </summary>
        public void ShowError(string message)
        {
            Show(message, ToastType.Error);
        }

        /// <summary>
        /// 自动隐藏窗口 Toast
        /// </summary>
        private async Task AutoHideWindowToast(int durationMs)
        {
            await Task.Delay(durationMs);
            HideWindowToast();
        }

        /// <summary>
        /// 隐藏窗口 Toast
        /// </summary>
        private void HideWindowToast()
        {
            if (_toastWindow == null || !_isShowing) return;

            _toastWindow.AnimateHide(() =>
            {
                _toastWindow?.Close();
                _toastWindow = null;
                _isShowing = false;
            });
        }
    }
}
