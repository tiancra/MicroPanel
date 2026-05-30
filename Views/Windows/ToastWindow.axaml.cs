using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using MicroPanel.Controls;

namespace MicroPanel.Views.Windows
{
    public partial class ToastWindow : MyWindow
    {
        private Border? _toastBorder;
        private TextBlock? _toastText;
        private int _targetY = 0; // 动画目标 Y 位置

        public ToastWindow()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _toastBorder = this.FindControl<Border>("ToastBorder");
            _toastText = this.FindControl<TextBlock>("ToastText");
        }

        /// <summary>
        /// 设置 Toast 内容和样式
        /// </summary>
        public void SetToast(string message, SolidColorBrush background, IBrush foreground)
        {
            if (_toastBorder != null)
            {
                _toastBorder.Background = background;
            }
            if (_toastText != null)
            {
                _toastText.Text = message;
                _toastText.Foreground = foreground;
            }
        }

        /// <summary>
        /// 设置动画目标位置
        /// </summary>
        public void SetTargetY(int targetY)
        {
            _targetY = targetY;
        }

        /// <summary>
        /// 显示动画（从顶部滑入）
        /// </summary>
        public void AnimateShow()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            var duration = 300;
            var startTime = DateTime.Now;

            this.Opacity = 1;

            // 初始位置（在目标位置上方 50 像素）
            int startY = _targetY - 50;

            timer.Tick += (s, e) =>
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var progress = elapsed / duration;

                if (progress >= 1.0)
                {
                    timer.Stop();
                    this.Position = new PixelPoint(this.Position.X, _targetY);
                    return;
                }

                // Ease Out Cubic
                var easeProgress = 1 - Math.Pow(1 - progress, 3);
                int currentY = (int)(startY + (_targetY - startY) * easeProgress);

                this.Position = new PixelPoint(this.Position.X, currentY);
            };

            timer.Start();
        }

        /// <summary>
        /// 隐藏动画（向上滑出）
        /// </summary>
        public void AnimateHide(Action onComplete)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            var duration = 250;
            var startTime = DateTime.Now;
            int startY = this.Position.Y;

            timer.Tick += (s, e) =>
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var progress = elapsed / duration;

                if (progress >= 1.0)
                {
                    timer.Stop();
                    this.Opacity = 0;
                    onComplete?.Invoke();
                    return;
                }

                // Ease Out Cubic
                var easeProgress = 1 - Math.Pow(1 - progress, 3);
                int currentY = (int)(startY - 50 * easeProgress);

                this.Position = new PixelPoint(this.Position.X, currentY);
            };

            timer.Start();
        }
    }
}
