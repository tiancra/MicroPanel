using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.IO;
using System.Text.Json;

namespace MicroPanel.Services
{
    public enum ThemeType
    {
        Light = 0,
        Dark = 1,
        System = 2
    }

    public class ThemeSettings
    {
        public ThemeType Theme { get; set; } = ThemeType.System;
    }

    public class ThemeService
    {
        private static readonly Lazy<ThemeService> _instance = new(() => new ThemeService());
        public static ThemeService Instance => _instance.Value;

        private const string SettingsFileName = "settings.json";
        private string _settingsFilePath;

        public ThemeType CurrentTheme { get; private set; } = ThemeType.System;

        private ThemeService()
        {
            _settingsFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MicroPanel",
                SettingsFileName);

            LoadThemeSetting();
            ApplyTheme(CurrentTheme);
        }

        public void LoadThemeSetting()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<ThemeSettings>(json);
                    if (settings != null)
                    {
                        CurrentTheme = settings.Theme;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载主题设置失败: {ex.Message}");
                CurrentTheme = ThemeType.System;
            }
        }

        public void SaveThemeSetting()
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var settings = new ThemeSettings { Theme = CurrentTheme };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存主题设置失败: {ex.Message}");
            }
        }

        public void SetTheme(ThemeType theme)
        {
            CurrentTheme = theme;
            ApplyTheme(theme);
        }

        private void ApplyTheme(ThemeType theme)
        {
            var app = Application.Current;
            if (app == null) return;

            ThemeVariant targetVariant;

            switch (theme)
            {
                case ThemeType.Light:
                    targetVariant = ThemeVariant.Light;
                    break;
                case ThemeType.Dark:
                    targetVariant = ThemeVariant.Dark;
                    break;
                case ThemeType.System:
                default:
                    targetVariant = ThemeVariant.Default;
                    break;
            }

            app.RequestedThemeVariant = targetVariant;

            System.Diagnostics.Debug.WriteLine($"主题已切换为: {theme}");
        }

        public bool IsDarkTheme()
        {
            if (CurrentTheme == ThemeType.System)
            {
                // 检测系统主题
                return IsSystemDarkTheme();
            }
            return CurrentTheme == ThemeType.Dark;
        }

        private bool IsSystemDarkTheme()
        {
            try
            {
                // Windows 注册表检测系统主题
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("AppsUseLightTheme");
                        if (value is int intValue)
                        {
                            return intValue == 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检测系统主题失败: {ex.Message}");
            }

            // 默认返回 false（浅色主题）
            return false;
        }
    }
}
