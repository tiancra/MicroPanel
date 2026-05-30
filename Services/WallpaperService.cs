using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace MicroPanel.Services
{
    /// <summary>
    /// 桌面壁纸设置
    /// </summary>
    public class WallpaperSettings
    {
        /// <summary>
        /// 是否启用桌面壁纸显示
        /// </summary>
        public bool EnableDesktopWallpaper { get; set; } = false;

        /// <summary>
        /// 壁纸透明度 (0-100)
        /// </summary>
        public int WallpaperOpacity { get; set; } = 50;

        /// <summary>
        /// 是否启用模糊效果
        /// </summary>
        public bool EnableBlur { get; set; } = false;
    }

    /// <summary>
    /// 桌面壁纸服务
    /// </summary>
    public class WallpaperService
    {
        private static readonly Lazy<WallpaperService> _instance = new(() => new WallpaperService());
        public static WallpaperService Instance => _instance.Value;

        private const string SettingsFileName = "wallpaper_settings.json";
        private string _settingsFilePath;

        public WallpaperSettings Settings { get; private set; } = new();

        /// <summary>
        /// 壁纸变更事件
        /// </summary>
        public event EventHandler? WallpaperChanged;

        private WallpaperService()
        {
            _settingsFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MicroPanel",
                SettingsFileName);

            LoadSettings();
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<WallpaperSettings>(json);
                    if (settings != null)
                    {
                        Settings = settings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载壁纸设置失败: {ex.Message}");
                Settings = new WallpaperSettings();
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_settingsFilePath, json);

                // 触发变更事件
                WallpaperChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存壁纸设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置是否启用桌面壁纸
        /// </summary>
        public void SetEnableDesktopWallpaper(bool enable)
        {
            Settings.EnableDesktopWallpaper = enable;
            SaveSettings();
        }

        /// <summary>
        /// 设置壁纸透明度
        /// </summary>
        public void SetWallpaperOpacity(int opacity)
        {
            Settings.WallpaperOpacity = Math.Clamp(opacity, 0, 100);
            SaveSettings();
        }

        /// <summary>
        /// 设置是否启用模糊
        /// </summary>
        public void SetEnableBlur(bool enable)
        {
            Settings.EnableBlur = enable;
            SaveSettings();
        }

        /// <summary>
        /// 获取当前系统壁纸路径
        /// </summary>
        public string? GetSystemWallpaperPath()
        {
            try
            {
                // 从注册表读取当前壁纸路径
                using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                {
                    if (key != null)
                    {
                        var wallpaperPath = key.GetValue("WallPaper") as string;
                        if (!string.IsNullOrEmpty(wallpaperPath) && File.Exists(wallpaperPath))
                        {
                            return wallpaperPath;
                        }
                    }
                }

                // 尝试从Windows壁纸文件夹读取
                string windowsWallpaperPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Windows\Themes\TranscodedWallpaper");

                if (File.Exists(windowsWallpaperPath))
                {
                    return windowsWallpaperPath;
                }

                // 使用SystemParametersInfo获取壁纸
                string tempPath = Path.Combine(Path.GetTempPath(), "micropanel_wallpaper.jpg");
                if (GetWallpaper(tempPath))
                {
                    return tempPath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取系统壁纸失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 加载壁纸图片
        /// </summary>
        public Bitmap? LoadWallpaperBitmap()
        {
            try
            {
                var wallpaperPath = GetSystemWallpaperPath();
                if (!string.IsNullOrEmpty(wallpaperPath) && File.Exists(wallpaperPath))
                {
                    return new Bitmap(wallpaperPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载壁纸图片失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 获取壁纸不透明度 (0.0 - 1.0)
        /// </summary>
        public double GetOpacity()
        {
            return Settings.WallpaperOpacity / 100.0;
        }

        #region Windows API

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_GETDESKWALLPAPER = 0x0073;
        private const int MAX_PATH = 260;

        private bool GetWallpaper(string path)
        {
            try
            {
                char[] buffer = new char[MAX_PATH];
                bool result = SystemParametersInfo(SPI_GETDESKWALLPAPER, MAX_PATH, path, 0);
                return result && File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
