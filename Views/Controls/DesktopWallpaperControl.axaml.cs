using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MicroPanel.Services;
using System;

namespace MicroPanel.Views.Controls
{
    public partial class DesktopWallpaperControl : UserControl
    {
        private Image? _wallpaperImage;
        private Image? _blurredWallpaperImage;
        private Border? _overlayBorder;

        public DesktopWallpaperControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _wallpaperImage = this.FindControl<Image>("WallpaperImage");
            _blurredWallpaperImage = this.FindControl<Image>("BlurredWallpaperImage");
            _overlayBorder = this.FindControl<Border>("OverlayBorder");

            // 订阅壁纸变更事件
            WallpaperService.Instance.WallpaperChanged += OnWallpaperChanged;

            // 初始加载
            UpdateWallpaper();
        }

        private void OnUnloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // 取消订阅
            WallpaperService.Instance.WallpaperChanged -= OnWallpaperChanged;
        }

        private void OnWallpaperChanged(object? sender, System.EventArgs e)
        {
            UpdateWallpaper();
        }

        private void UpdateWallpaper()
        {
            var settings = WallpaperService.Instance.Settings;

            // 检查是否是桌面模式 - 非桌面模式下不显示壁纸
            if (!DesktopModeManager.Instance.IsDesktopMode)
            {
                if (_wallpaperImage != null)
                {
                    _wallpaperImage.Source = null;
                    _wallpaperImage.IsVisible = false;
                }
                if (_blurredWallpaperImage != null)
                {
                    _blurredWallpaperImage.Source = null;
                    _blurredWallpaperImage.IsVisible = false;
                }
                if (_overlayBorder != null)
                {
                    _overlayBorder.IsVisible = false;
                }
                return;
            }

            // 检查是否启用壁纸
            if (!settings.EnableDesktopWallpaper)
            {
                if (_wallpaperImage != null)
                {
                    _wallpaperImage.Source = null;
                    _wallpaperImage.IsVisible = false;
                }
                if (_blurredWallpaperImage != null)
                {
                    _blurredWallpaperImage.Source = null;
                    _blurredWallpaperImage.IsVisible = false;
                }
                if (_overlayBorder != null)
                {
                    _overlayBorder.IsVisible = false;
                }
                return;
            }

            // 加载壁纸图片
            var bitmap = WallpaperService.Instance.LoadWallpaperBitmap();
            if (bitmap != null)
            {
                // 根据是否启用模糊来决定显示哪个图片
                if (settings.EnableBlur)
                {
                    // 显示模糊层，隐藏原图
                    if (_wallpaperImage != null)
                    {
                        _wallpaperImage.Source = null;
                        _wallpaperImage.IsVisible = false;
                    }
                    if (_blurredWallpaperImage != null)
                    {
                        _blurredWallpaperImage.Source = bitmap;
                        _blurredWallpaperImage.IsVisible = true;
                        _blurredWallpaperImage.Opacity = WallpaperService.Instance.GetOpacity();
                    }
                }
                else
                {
                    // 显示原图，隐藏模糊层
                    if (_wallpaperImage != null)
                    {
                        _wallpaperImage.Source = bitmap;
                        _wallpaperImage.IsVisible = true;
                        _wallpaperImage.Opacity = WallpaperService.Instance.GetOpacity();
                    }
                    if (_blurredWallpaperImage != null)
                    {
                        _blurredWallpaperImage.Source = null;
                        _blurredWallpaperImage.IsVisible = false;
                    }
                }
            }
            else
            {
                // 加载失败，全部隐藏
                if (_wallpaperImage != null)
                {
                    _wallpaperImage.Source = null;
                    _wallpaperImage.IsVisible = false;
                }
                if (_blurredWallpaperImage != null)
                {
                    _blurredWallpaperImage.Source = null;
                    _blurredWallpaperImage.IsVisible = false;
                }
            }

            // 更新遮罩层 - 启用模糊时显示遮罩增强可读性
            if (_overlayBorder != null)
            {
                _overlayBorder.IsVisible = settings.EnableBlur;
                // 根据透明度设置调整遮罩不透明度
                double opacity = (100 - settings.WallpaperOpacity) / 100.0 * 0.5;
                _overlayBorder.Opacity = Math.Clamp(opacity, 0.1, 0.7);
            }
        }
    }
}
