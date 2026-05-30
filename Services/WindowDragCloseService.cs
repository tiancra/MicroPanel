using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Diagnostics;

namespace MicroPanel.Services
{
    /// <summary>
    /// 窗口拖动关闭服务 - 实现从顶部向下拖动关闭窗口的跟手动画
    /// </summary>
    public class WindowDragCloseService
    {
        private Window? _window;
        private bool _isDragging = false;
        private bool _isInCloseZone = false;
        private double _dragStartY;
        private double _windowStartYDouble;
        private int _windowStartY;
        private double _dragThreshold = 150; // 拖动超过此距离触发关闭
        private const double CloseZoneHeight = 50; // 顶部触发区域高度（从屏幕顶部向下50像素）
        private Border? _closeIndicator;
        private TextBlock? _closeText;

        /// <summary>
        /// 为窗口启用拖动关闭功能
        /// </summary>
        public void EnableForWindow(Window window)
        {
            _window = window;

            // 创建关闭提示指示器
            CreateCloseIndicator();

            // 订阅鼠标事件
            window.PointerPressed += OnPointerPressed;
            window.PointerMoved += OnPointerMoved;
            window.PointerReleased += OnPointerReleased;

            // 窗口关闭时清理
            window.Closed += (s, e) =>
            {
                _closeIndicator = null;
                _closeText = null;
            };
        }

        /// <summary>
        /// 创建关闭提示指示器
        /// </summary>
        private void CreateCloseIndicator()
        {
            if (_window == null) return;

            // 创建关闭提示层
            _closeIndicator = new Border
            {
                Background = new SolidColorBrush(Colors.Red) { Opacity = 0 },
                Height = 60,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                IsVisible = false,
                ZIndex = 9999
            };

            _closeText = new TextBlock
            {
                Text = "↓ 继续下拉关闭窗口",
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Opacity = 0
            };

            _closeIndicator.Child = _closeText;

            // 添加到窗口内容
            if (_window.Content is Panel panel)
            {
                panel.Children.Add(_closeIndicator);
            }
            else if (_window.Content is Control control)
            {
                // 如果内容不是 Panel，需要包装
                var grid = new Grid();
                grid.Children.Add(control);
                grid.Children.Add(_closeIndicator);
                _window.Content = grid;
            }
        }

        /// <summary>
        /// 指针按下事件
        /// </summary>
        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_window == null) return;

            var position = e.GetPosition(_window);

            // 检查是否在顶部关闭区域
            if (position.Y <= CloseZoneHeight)
            {
                _isInCloseZone = true;
                _isDragging = true;
                _dragStartY = position.Y;
                _windowStartY = _window.Position.Y;
                _windowStartYDouble = _window.Position.Y;

                // 显示关闭提示
                ShowCloseIndicator();

                e.Handled = true;
            }
        }

        /// <summary>
        /// 指针移动事件 - 跟手动画
        /// </summary>
        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isDragging || _window == null) return;

            var position = e.GetPosition(_window);
            var deltaY = position.Y - _dragStartY;

            // 只响应向下拖动
            if (deltaY > 0)
            {
                // 计算拖动进度 (0 - 1)
                var progress = Math.Min(deltaY / _dragThreshold, 1.0);

                // 更新窗口位置（跟手效果）
                int newY = (int)(_windowStartYDouble + deltaY * 0.5); // 0.5 阻尼系数，让拖动更自然
                int newX = _window.Position.X;
                _window.Position = new PixelPoint(newX, newY);

                // 更新关闭提示
                UpdateCloseIndicator(progress);

                // 如果超过阈值，改变提示文字
                if (_closeText != null && progress >= 1.0)
                {
                    _closeText.Text = "松开关闭窗口";
                    _closeText.Foreground = Brushes.White;
                }
                else if (_closeText != null)
                {
                    _closeText.Text = "↓ 继续下拉关闭窗口";
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// 指针释放事件
        /// </summary>
        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isDragging || _window == null) return;

            var position = e.GetPosition(_window);
            var deltaY = position.Y - _dragStartY;

            // 检查是否超过关闭阈值
            if (deltaY >= _dragThreshold)
            {
                // 执行关闭动画
                AnimateAndCloseWindow();
            }
            else
            {
                // 恢复原位动画
                AnimateRestoreWindow();
            }

            _isDragging = false;
            _isInCloseZone = false;

            e.Handled = true;
        }

        /// <summary>
        /// 显示关闭提示
        /// </summary>
        private void ShowCloseIndicator()
        {
            if (_closeIndicator == null || _closeText == null) return;

            _closeIndicator.IsVisible = true;
            _closeIndicator.Background = new SolidColorBrush(Colors.Red) { Opacity = 0.3 };
            _closeText.Opacity = 0.8;
        }

        /// <summary>
        /// 更新关闭提示
        /// </summary>
        private void UpdateCloseIndicator(double progress)
        {
            if (_closeIndicator == null || _closeText == null) return;

            // 背景透明度随进度增加
            var opacity = 0.3 + (progress * 0.5);
            _closeIndicator.Background = new SolidColorBrush(Colors.Red) { Opacity = opacity };

            // 文字透明度
            _closeText.Opacity = 0.8 + (progress * 0.2);

            // 背景高度随进度增加
            _closeIndicator.Height = 60 + (progress * 40);
        }

        /// <summary>
        /// 隐藏关闭提示
        /// </summary>
        private void HideCloseIndicator()
        {
            if (_closeIndicator == null) return;

            _closeIndicator.IsVisible = false;
        }

        /// <summary>
        /// 执行关闭动画并关闭窗口
        /// </summary>
        private void AnimateAndCloseWindow()
        {
            if (_window == null) return;

            // 使用 DispatcherTimer 实现简单动画
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
            double startY = _window.Position.Y;
            double screenHeight = _window.Screens?.Primary?.Bounds.Height ?? 1080;
            var duration = 200; // 动画持续时间 ms
            var startTime = DateTime.Now;

            timer.Tick += (s, e) =>
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var progress = elapsed / duration;

                if (progress >= 1.0)
                {
                    timer.Stop();
                    _window?.Close();
                    return;
                }

                // 缓动函数 - ease out
                var easeProgress = 1 - Math.Pow(1 - progress, 3);
                int newY = (int)(startY + (screenHeight - startY) * easeProgress);

                if (_window != null)
                {
                    int currentX = _window.Position.X;
                    _window.Position = new PixelPoint(currentX, newY);

                    // 同时降低窗口透明度
                    _window.Opacity = 1 - (easeProgress * 0.5);
                }
            };

            timer.Start();
        }

        /// <summary>
        /// 恢复原位动画
        /// </summary>
        private void AnimateRestoreWindow()
        {
            if (_window == null) return;

            HideCloseIndicator();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            double startY = _window.Position.Y;
            double targetY = _windowStartYDouble;
            var duration = 300; // 恢复动画稍慢一些，更有弹性
            var startTime = DateTime.Now;

            timer.Tick += (s, e) =>
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var progress = elapsed / duration;

                if (progress >= 1.0)
                {
                    timer.Stop();
                    if (_window != null)
                    {
                        int finalX = _window.Position.X;
                        _window.Position = new PixelPoint(finalX, _windowStartY);
                        _window.Opacity = 1;
                    }
                    return;
                }

                // 弹性缓动
                var easeProgress = ElasticEase(progress);
                int newY = (int)(startY + (targetY - startY) * easeProgress);

                if (_window != null)
                {
                    int currentX = _window.Position.X;
                    _window.Position = new PixelPoint(currentX, newY);
                    _window.Opacity = 1;
                }
            };

            timer.Start();
        }

        /// <summary>
        /// 弹性缓动函数
        /// </summary>
        private double ElasticEase(double t)
        {
            if (t == 0) return 0;
            if (t == 1) return 1;

            var p = 0.3;
            var s = p / 4;
            return Math.Pow(2, -10 * t) * Math.Sin((t - s) * (2 * Math.PI) / p) + 1;
        }
    }
}
