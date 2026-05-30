using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace MicroPanel.Services
{
    /// <summary>
    /// 导航历史记录服务 - 记录页面访问历史，支持返回上一级
    /// </summary>
    public class NavigationHistoryService
    {
        private static NavigationHistoryService? _instance;
        public static NavigationHistoryService Instance => _instance ??= new NavigationHistoryService();

        private Stack<string> _history = new Stack<string>();
        private Window? _mainWindow;
        private bool _isDesktopMode = false;
        private const int EdgeSwipeWidth = 50; // 屏幕边缘滑动范围
        private const int SwipeThreshold = 80; // 触发返回的滑动阈值（等于指示器宽度）
        private bool _isSwiping = false;
        private double _swipeStartX;
        private bool _isLeftEdgeSwipe = false; // 是否从左边缘滑动

        // 滑动指示器
        private Border? _leftSwipeIndicator;
        private Border? _rightSwipeIndicator;
        private bool _isIndicatorShown = false;

        /// <summary>
        /// 初始化导航历史服务
        /// </summary>
        public void Initialize(Window mainWindow, bool isDesktopMode)
        {
            _mainWindow = mainWindow;
            _isDesktopMode = isDesktopMode;

            if (isDesktopMode)
            {
                // 桌面模式：启用边缘滑动检测
                SetupEdgeSwipeDetection();
                // 延迟创建指示器，等待视觉树准备好
                Dispatcher.UIThread.Post(CreateSwipeIndicator, DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// 创建滑动指示器
        /// </summary>
        private void CreateSwipeIndicator()
        {
            if (_mainWindow == null) return;

            // 创建左边缘指示器
            _leftSwipeIndicator = new Border
            {
                Width = 80,
                Background = new SolidColorBrush(Color.Parse("#333333")) { Opacity = 0.6 },
                CornerRadius = new CornerRadius(0, 12, 12, 0),
                IsVisible = false,
                ZIndex = 9998,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                Margin = new Thickness(-80, 0, 0, 0)
            };

            _leftSwipeIndicator.Child = new TextBlock
            {
                Text = "‹",
                FontSize = 64,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            // 创建右边缘指示器
            _rightSwipeIndicator = new Border
            {
                Width = 80,
                Background = new SolidColorBrush(Color.Parse("#333333")) { Opacity = 0.6 },
                CornerRadius = new CornerRadius(12, 0, 0, 12),
                IsVisible = false,
                ZIndex = 9998,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                Margin = new Thickness(0, 0, -80, 0)
            };

            _rightSwipeIndicator.Child = new TextBlock
            {
                Text = "›",
                FontSize = 64,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            // 添加指示器到窗口
            AddIndicatorsToWindow();
        }

        /// <summary>
        /// 添加指示器到窗口
        /// </summary>
        private void AddIndicatorsToWindow()
        {
            if (_mainWindow == null || _leftSwipeIndicator == null || _rightSwipeIndicator == null) return;

            // 如果窗口内容已经是 Grid，直接添加
            if (_mainWindow.Content is Grid grid)
            {
                if (!grid.Children.Contains(_leftSwipeIndicator))
                {
                    grid.Children.Add(_leftSwipeIndicator);
                    grid.Children.Add(_rightSwipeIndicator);
                }
                return;
            }

            // 如果窗口内容是 Panel，直接添加
            if (_mainWindow.Content is Panel panel)
            {
                if (!panel.Children.Contains(_leftSwipeIndicator))
                {
                    panel.Children.Add(_leftSwipeIndicator);
                    panel.Children.Add(_rightSwipeIndicator);
                }
                return;
            }

            // 如果窗口内容是 Control，创建 Canvas 覆盖层
            var originalContent = _mainWindow.Content as Control;
            if (originalContent != null)
            {
                // 保存原内容
                _mainWindow.Content = null;

                // 创建 Canvas 作为容器
                var canvas = new Canvas();
                canvas.Children.Add(originalContent);

                // 设置原内容填充整个 Canvas
                originalContent.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                originalContent.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

                // 添加指示器到 Canvas
                canvas.Children.Add(_leftSwipeIndicator);
                canvas.Children.Add(_rightSwipeIndicator);

                // 设置指示器位置
                Canvas.SetLeft(_leftSwipeIndicator, -80);
                Canvas.SetTop(_leftSwipeIndicator, 0);
                _leftSwipeIndicator.Height = double.NaN; // Auto height

                Canvas.SetRight(_rightSwipeIndicator, -80);
                Canvas.SetTop(_rightSwipeIndicator, 0);
                _rightSwipeIndicator.Height = double.NaN; // Auto height

                // 绑定高度到 Canvas
                canvas.SizeChanged += (s, e) =>
                {
                    if (canvas.Bounds.Height > 0)
                    {
                        _leftSwipeIndicator.Height = canvas.Bounds.Height;
                        _rightSwipeIndicator.Height = canvas.Bounds.Height;
                    }
                };

                _mainWindow.Content = canvas;
            }
        }

        /// <summary>
        /// 处理键盘事件（由主窗口调用）
        /// </summary>
        public bool HandleKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                System.Diagnostics.Debug.WriteLine("导航历史: 检测到 Esc 键，执行返回");
                if (GoBack())
                {
                    e.Handled = true;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 记录页面访问
        /// </summary>
        public void Push(string pageName)
        {
            if (string.IsNullOrEmpty(pageName)) return;

            // 如果当前页面与上一个页面相同，不重复记录
            if (_history.Count > 0 && _history.Peek() == pageName)
            {
                return;
            }

            _history.Push(pageName);
            System.Diagnostics.Debug.WriteLine($"导航历史: 添加 {pageName}, 当前深度: {_history.Count}");
        }

        /// <summary>
        /// 返回上一级
        /// </summary>
        public bool GoBack()
        {
            if (_history.Count <= 1)
            {
                System.Diagnostics.Debug.WriteLine("导航历史: 已经是第一页，无法返回");
                return false;
            }

            // 弹出当前页面
            var currentPage = _history.Pop();
            System.Diagnostics.Debug.WriteLine($"导航历史: 返回，移除 {currentPage}");

            // 获取上一级页面
            var previousPage = _history.Peek();
            System.Diagnostics.Debug.WriteLine($"导航历史: 跳转到 {previousPage}");

            // 触发返回事件
            OnNavigateBack?.Invoke(this, previousPage);

            return true;
        }

        /// <summary>
        /// 获取当前页面
        /// </summary>
        public string? GetCurrentPage()
        {
            return _history.Count > 0 ? _history.Peek() : null;
        }

        /// <summary>
        /// 获取历史记录数量
        /// </summary>
        public int Count => _history.Count;

        /// <summary>
        /// 清空历史记录
        /// </summary>
        public void Clear()
        {
            _history.Clear();
            System.Diagnostics.Debug.WriteLine("导航历史: 已清空");
        }

        /// <summary>
        /// 返回导航事件
        /// </summary>
        public event EventHandler<string>? OnNavigateBack;

        /// <summary>
        /// 设置边缘滑动检测（桌面模式）
        /// </summary>
        private void SetupEdgeSwipeDetection()
        {
            if (_mainWindow == null) return;

            _mainWindow.PointerPressed += OnEdgePointerPressed;
            _mainWindow.PointerMoved += OnEdgePointerMoved;
            _mainWindow.PointerReleased += OnEdgePointerReleased;
        }

        /// <summary>
        /// 边缘滑动检测 - 指针按下
        /// </summary>
        private void OnEdgePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_mainWindow == null) return;

            var position = e.GetPosition(_mainWindow);
            var screenPosition = _mainWindow.PointToScreen(position);

            // 检查是否在屏幕左边缘或右边缘
            var screen = _mainWindow.Screens?.Primary;
            if (screen == null) return;

            var bounds = screen.Bounds;
            var isLeftEdge = screenPosition.X <= bounds.X + EdgeSwipeWidth;
            // 关闭右侧返回：只检测左边缘
            // var isRightEdge = screenPosition.X >= bounds.X + bounds.Width - EdgeSwipeWidth;

            if (isLeftEdge)
            {
                _isSwiping = true;
                _isLeftEdgeSwipe = true;
                _swipeStartX = screenPosition.X;
                _isIndicatorShown = false;

                System.Diagnostics.Debug.WriteLine($"边缘滑动: 开始检测，左边缘，位置: {screenPosition.X}");
            }
        }

        /// <summary>
        /// 边缘滑动检测 - 指针移动
        /// </summary>
        private void OnEdgePointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isSwiping || _mainWindow == null) return;

            var position = e.GetPosition(_mainWindow);
            var screenPosition = _mainWindow.PointToScreen(position);

            // 计算滑动距离
            var deltaX = screenPosition.X - _swipeStartX;

            // 只响应向内侧滑动
            if (_isLeftEdgeSwipe)
            {
                // 从左边缘开始，需要向右滑动（deltaX > 0）
                if (deltaX <= 0)
                {
                    // 方向不对，忽略
                    return;
                }
            }
            else
            {
                // 从右边缘开始，需要向左滑动（deltaX < 0）
                if (deltaX >= 0)
                {
                    // 方向不对，忽略
                    return;
                }
                // 转换为正数方便计算
                deltaX = -deltaX;
            }

            // 显示并更新指示器位置（跟手效果）
            if (deltaX > 0)
            {
                UpdateSwipeIndicator(deltaX);
            }
        }

        /// <summary>
        /// 边缘滑动检测 - 指针释放（松手返回）
        /// </summary>
        private void OnEdgePointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isSwiping) return;

            var position = e.GetPosition(_mainWindow);
            var screenPosition = _mainWindow.PointToScreen(position);
            var deltaX = screenPosition.X - _swipeStartX;

            // 根据滑动方向计算实际距离
            if (_isLeftEdgeSwipe)
            {
                // 左边缘：向右滑动，deltaX 应该为正
                if (deltaX < 0) deltaX = 0;
            }
            else
            {
                // 右边缘：向左滑动，deltaX 应该为负
                deltaX = -deltaX;
                if (deltaX < 0) deltaX = 0;
            }

            // 松手返回：如果超过阈值则返回，否则恢复
            if (deltaX >= SwipeThreshold)
            {
                System.Diagnostics.Debug.WriteLine("边缘滑动: 松手返回");
                HideSwipeIndicator();
                GoBack();
            }
            else
            {
                AnimateIndicatorBack();
            }

            _isSwiping = false;
        }

        /// <summary>
        /// 更新滑动指示器位置（跟手效果）
        /// </summary>
        private void UpdateSwipeIndicator(double deltaX)
        {
            // 限制最大移动距离
            var maxMove = Math.Min(deltaX, SwipeThreshold);
            var progress = maxMove / SwipeThreshold;

            // 显示并更新对应的指示器
            if (_isLeftEdgeSwipe)
            {
                if (_leftSwipeIndicator == null) return;

                if (!_isIndicatorShown)
                {
                    _leftSwipeIndicator.IsVisible = true;
                    _isIndicatorShown = true;
                }

                // 左边缘：从左边滑入
                _leftSwipeIndicator.Margin = new Thickness(-80 + maxMove, 0, 0, 0);

                // 更新背景透明度
                var opacity = 0.6 + (progress * 0.3);
                _leftSwipeIndicator.Background = new SolidColorBrush(Color.Parse("#333333")) { Opacity = opacity };
            }
            else
            {
                if (_rightSwipeIndicator == null) return;

                if (!_isIndicatorShown)
                {
                    _rightSwipeIndicator.IsVisible = true;
                    _isIndicatorShown = true;
                }

                // 右边缘：从右边滑入
                _rightSwipeIndicator.Margin = new Thickness(0, 0, -80 + maxMove, 0);

                // 更新背景透明度
                var opacity = 0.6 + (progress * 0.3);
                _rightSwipeIndicator.Background = new SolidColorBrush(Color.Parse("#333333")) { Opacity = opacity };
            }
        }

        /// <summary>
        /// 隐藏滑动指示器
        /// </summary>
        private void HideSwipeIndicator()
        {
            if (_isLeftEdgeSwipe)
            {
                if (_leftSwipeIndicator != null)
                    _leftSwipeIndicator.IsVisible = false;
            }
            else
            {
                if (_rightSwipeIndicator != null)
                    _rightSwipeIndicator.IsVisible = false;
            }
            _isIndicatorShown = false;
        }

        /// <summary>
        /// 指示器恢复动画（未达到阈值时）
        /// </summary>
        private void AnimateIndicatorBack()
        {
            var indicator = _isLeftEdgeSwipe ? _leftSwipeIndicator : _rightSwipeIndicator;
            if (indicator == null) return;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            var startMargin = _isLeftEdgeSwipe
                ? indicator.Margin.Left
                : indicator.Margin.Right;
            var duration = 200;
            var startTime = DateTime.Now;

            timer.Tick += (s, e) =>
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var progress = elapsed / duration;

                if (progress >= 1.0)
                {
                    timer.Stop();
                    indicator.IsVisible = false;
                    _isIndicatorShown = false;
                    return;
                }

                // Ease Out Cubic
                var easeProgress = 1 - Math.Pow(1 - progress, 3);
                var currentMargin = startMargin - (startMargin + 80) * easeProgress;

                if (_isLeftEdgeSwipe)
                {
                    indicator.Margin = new Thickness(currentMargin, 0, 0, 0);
                }
                else
                {
                    indicator.Margin = new Thickness(0, 0, currentMargin, 0);
                }
            };

            timer.Start();
        }
    }
}
