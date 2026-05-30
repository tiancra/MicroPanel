using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace MicroPanel.Services
{
    /// <summary>
    /// Toast 通知服务
    /// 使用内嵌控件，在窗口顶部显示
    /// </summary>
    public class ToastService
    {
        private static ToastService? _instance;
        public static ToastService Instance => _instance ??= new ToastService();

        private Window? _hostWindow;
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
    }
}
