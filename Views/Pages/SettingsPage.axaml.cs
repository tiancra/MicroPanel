using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MicroPanelAvalonia.Services;

namespace MicroPanelAvalonia.Views.Pages
{
    public partial class SettingsPage : UserControl
    {
        private ComboBox? _themeComboBox;

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
    }
}
