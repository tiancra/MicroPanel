using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MicroPanel.Services;

namespace MicroPanel.Views.Pages
{
    public partial class SettingsPage : UserControl
    {
        private ComboBox? _themeComboBox;
        private Border? _desktopWallpaperCard;
        private ToggleSwitch? _enableWallpaperToggle;
        private StackPanel? _wallpaperOptionsPanel;
        private Slider? _opacitySlider;
        private TextBlock? _opacityValueText;
        private ToggleSwitch? _enableBlurToggle;
        private TextBlock? _blurDescriptionText;

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // 初始化主题设置
            InitializeThemeSettings();
            
            // 初始化桌面壁纸设置
            InitializeWallpaperSettings();
        }

        #region 主题设置

        private void InitializeThemeSettings()
        {
            _themeComboBox = this.FindControl<ComboBox>("ThemeComboBox");
            if (_themeComboBox != null)
            {
                // 加载当前主题设置
                var currentTheme = ThemeService.Instance.CurrentTheme;
                _themeComboBox.SelectedIndex = (int)currentTheme;

                // 订阅选择变更事件
                _themeComboBox.SelectionChanged += OnThemeSelectionChanged;
            }
        }

        private void OnThemeSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_themeComboBox == null) return;

            var selectedIndex = _themeComboBox.SelectedIndex;
            var theme = (ThemeType)selectedIndex;

            // 应用主题
            ThemeService.Instance.SetTheme(theme);

            // 保存设置
            ThemeService.Instance.SaveThemeSetting();
        }

        #endregion

        #region 桌面壁纸设置

        private void InitializeWallpaperSettings()
        {
            // 检查是否在桌面模式
            bool isDesktopMode = DesktopModeManager.Instance.IsDesktopMode;

            _desktopWallpaperCard = this.FindControl<Border>("DesktopWallpaperCard");
            if (_desktopWallpaperCard != null)
            {
                // 仅在桌面模式下显示壁纸设置
                _desktopWallpaperCard.IsVisible = isDesktopMode;
            }

            if (!isDesktopMode) return;

            // 获取控件引用
            _enableWallpaperToggle = this.FindControl<ToggleSwitch>("EnableWallpaperToggle");
            _wallpaperOptionsPanel = this.FindControl<StackPanel>("WallpaperOptionsPanel");
            _opacitySlider = this.FindControl<Slider>("OpacitySlider");
            _opacityValueText = this.FindControl<TextBlock>("OpacityValueText");
            _enableBlurToggle = this.FindControl<ToggleSwitch>("EnableBlurToggle");
            _blurDescriptionText = this.FindControl<TextBlock>("BlurDescriptionText");

            // 加载当前设置
            LoadWallpaperSettings();

            // 订阅事件
            SubscribeWallpaperEvents();
        }

        private void LoadWallpaperSettings()
        {
            var settings = WallpaperService.Instance.Settings;

            if (_enableWallpaperToggle != null)
            {
                _enableWallpaperToggle.IsChecked = settings.EnableDesktopWallpaper;
            }

            if (_wallpaperOptionsPanel != null)
            {
                _wallpaperOptionsPanel.IsVisible = settings.EnableDesktopWallpaper;
            }

            if (_opacitySlider != null)
            {
                _opacitySlider.Value = settings.WallpaperOpacity;
            }

            if (_opacityValueText != null)
            {
                _opacityValueText.Text = $"{settings.WallpaperOpacity}%";
            }

            if (_enableBlurToggle != null)
            {
                _enableBlurToggle.IsChecked = settings.EnableBlur;
            }

            if (_blurDescriptionText != null)
            {
                _blurDescriptionText.IsVisible = settings.EnableBlur;
            }
        }

        private void SubscribeWallpaperEvents()
        {
            // 启用/禁用壁纸
            if (_enableWallpaperToggle != null)
            {
                _enableWallpaperToggle.IsCheckedChanged += (s, e) =>
                {
                    bool isEnabled = _enableWallpaperToggle.IsChecked ?? false;
                    WallpaperService.Instance.SetEnableDesktopWallpaper(isEnabled);
                    
                    if (_wallpaperOptionsPanel != null)
                    {
                        _wallpaperOptionsPanel.IsVisible = isEnabled;
                    }
                };
            }

            // 透明度滑块
            if (_opacitySlider != null)
            {
                _opacitySlider.ValueChanged += (s, e) =>
                {
                    int opacity = (int)_opacitySlider.Value;
                    WallpaperService.Instance.SetWallpaperOpacity(opacity);
                    
                    if (_opacityValueText != null)
                    {
                        _opacityValueText.Text = $"{opacity}%";
                    }
                };
            }

            // 模糊效果
            if (_enableBlurToggle != null)
            {
                _enableBlurToggle.IsCheckedChanged += (s, e) =>
                {
                    bool isEnabled = _enableBlurToggle.IsChecked ?? false;
                    WallpaperService.Instance.SetEnableBlur(isEnabled);
                    
                    if (_blurDescriptionText != null)
                    {
                        _blurDescriptionText.IsVisible = isEnabled;
                    }
                };
            }
        }

        #endregion
    }
}
