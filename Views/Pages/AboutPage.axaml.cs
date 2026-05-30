using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;

namespace MicroPanel.Views.Pages
{
    public partial class AboutPage : UserControl
    {
        private int _versionClickCount = 0;
        private DateTime _firstClickTime = DateTime.MinValue;
        private const int RequiredClicks = 5;
        private const int ClickTimeoutSeconds = 3;

        public AboutPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            LoadSystemInfo();
        }

        /// <summary>
        /// 加载系统信息
        /// </summary>
        private void LoadSystemInfo()
        {
            try
            {
                // CPU 信息
                var cpuInfo = GetCpuInfo();
                var cpuText = this.FindControl<TextBlock>("CpuInfoText");
                if (cpuText != null) cpuText.Text = cpuInfo;

                // 内存信息
                var memoryInfo = GetMemoryInfo();
                var memoryText = this.FindControl<TextBlock>("MemoryInfoText");
                if (memoryText != null) memoryText.Text = memoryInfo;

                // 磁盘信息
                var diskInfo = GetDiskInfo();
                var diskText = this.FindControl<TextBlock>("DiskInfoText");
                if (diskText != null) diskText.Text = diskInfo;

                // 系统版本
                var osVersion = GetOsVersion();
                var osText = this.FindControl<TextBlock>("OsVersionText");
                if (osText != null) osText.Text = osVersion;

                // 内核版本
                var kernelVersion = GetKernelVersion();
                var kernelText = this.FindControl<TextBlock>("KernelVersionText");
                if (kernelText != null) kernelText.Text = kernelVersion;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载系统信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取 CPU 详细信息
        /// </summary>
        private string GetCpuInfo()
        {
            try
            {
                var processorCount = Environment.ProcessorCount;
                string cpuName = "未知";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
                        foreach (var obj in searcher.Get())
                        {
                            cpuName = obj["Name"]?.ToString()?.Trim() ?? "未知";
                            break;
                        }
                    }
                    catch
                    {
                        // 如果 WMI 失败，尝试从注册表读取
                        cpuName = GetCpuNameFromRegistry() ?? "未知";
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // 从 /proc/cpuinfo 读取
                    if (File.Exists("/proc/cpuinfo"))
                    {
                        var lines = File.ReadAllLines("/proc/cpuinfo");
                        var modelNameLine = lines.FirstOrDefault(l => l.StartsWith("model name"));
                        if (modelNameLine != null)
                        {
                            var parts = modelNameLine.Split(':');
                            if (parts.Length >= 2)
                            {
                                cpuName = parts[1].Trim();
                            }
                        }
                    }
                }

                return $"{cpuName} ({processorCount} 核)";
            }
            catch
            {
                return $"{Environment.ProcessorCount} 核处理器";
            }
        }

        /// <summary>
        /// 从注册表获取 CPU 名称
        /// </summary>
        private string? GetCpuNameFromRegistry()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                return key?.GetValue("ProcessorNameString")?.ToString()?.Trim();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取内存信息
        /// </summary>
        private string GetMemoryInfo()
        {
            try
            {
                var totalMemory = GC.GetTotalMemory(false);
                // 获取工作集内存（进程使用的物理内存）
                var process = Process.GetCurrentProcess();
                var workingSet = process.WorkingSet64;
                var totalPhysical = GetTotalPhysicalMemory();

                if (totalPhysical > 0)
                {
                    return $"已用 {FormatBytes(workingSet)} / 总计 {FormatBytes(totalPhysical)}";
                }
                else
                {
                    return $"已用 {FormatBytes(workingSet)}";
                }
            }
            catch
            {
                return "未知";
            }
        }

        /// <summary>
        /// 获取总物理内存
        /// </summary>
        private long GetTotalPhysicalMemory()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                        foreach (var obj in searcher.Get())
                        {
                            var totalBytes = obj["TotalPhysicalMemory"];
                            if (totalBytes != null)
                            {
                                return Convert.ToInt64(totalBytes);
                            }
                        }
                    }
                    catch
                    {
                        // WMI 失败时使用 GC 信息
                    }

                    var gcInfo = GC.GetGCMemoryInfo();
                    return gcInfo.TotalAvailableMemoryBytes;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // 尝试从 /proc/meminfo 读取
                    if (File.Exists("/proc/meminfo"))
                    {
                        var lines = File.ReadAllLines("/proc/meminfo");
                        var memTotalLine = lines.FirstOrDefault(l => l.StartsWith("MemTotal:"));
                        if (memTotalLine != null)
                        {
                            var parts = memTotalLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2 && long.TryParse(parts[1], out var kb))
                            {
                                return kb * 1024;
                            }
                        }
                    }
                }

                // 使用 GC 信息作为备选
                var info = GC.GetGCMemoryInfo();
                return info.TotalAvailableMemoryBytes;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 获取磁盘信息
        /// </summary>
        private string GetDiskInfo()
        {
            try
            {
                var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.DriveType == DriveType.Fixed);
                if (drive != null)
                {
                    var used = drive.TotalSize - drive.AvailableFreeSpace;
                    return $"已用 {FormatBytes(used)} / 总计 {FormatBytes(drive.TotalSize)}";
                }
                return "未知";
            }
            catch
            {
                return "未知";
            }
        }

        /// <summary>
        /// 获取操作系统版本（友好名称）
        /// </summary>
        private string GetOsVersion()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return GetWindowsVersionName();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // 尝试读取 /etc/os-release
                    var osRelease = GetLinuxDistributionName();
                    if (!string.IsNullOrEmpty(osRelease))
                    {
                        return osRelease;
                    }
                }

                var osDescription = RuntimeInformation.OSDescription;
                var osArchitecture = RuntimeInformation.OSArchitecture;
                return $"{osDescription} ({osArchitecture})";
            }
            catch
            {
                return "未知";
            }
        }

        /// <summary>
        /// 获取 Windows 版本友好名称
        /// </summary>
        private string GetWindowsVersionName()
        {
            try
            {
                var version = Environment.OSVersion.Version;
                var major = version.Major;
                var minor = version.Minor;
                var build = version.Build;

                string versionName;

                if (major == 10 && build >= 22000)
                {
                    versionName = "Windows 11";
                }
                else if (major == 10)
                {
                    versionName = "Windows 10";
                }
                else if (major == 6 && minor == 3)
                {
                    versionName = "Windows 8.1";
                }
                else if (major == 6 && minor == 2)
                {
                    versionName = "Windows 8";
                }
                else if (major == 6 && minor == 1)
                {
                    versionName = "Windows 7";
                }
                else if (major == 6 && minor == 0)
                {
                    versionName = "Windows Vista";
                }
                else if (major == 5 && minor == 2)
                {
                    versionName = "Windows XP x64 / Server 2003";
                }
                else if (major == 5 && minor == 1)
                {
                    versionName = "Windows XP";
                }
                else
                {
                    versionName = $"Windows {major}.{minor}";
                }

                // 尝试获取显示版本（如 22H2）
                var displayVersion = GetWindowsDisplayVersion();
                if (!string.IsNullOrEmpty(displayVersion))
                {
                    versionName += $" {displayVersion}";
                }

                var arch = RuntimeInformation.OSArchitecture.ToString().ToLower();
                return $"{versionName} ({arch})";
            }
            catch
            {
                return "Windows 未知版本";
            }
        }

        /// <summary>
        /// 获取 Windows 显示版本（如 22H2）
        /// </summary>
        private string? GetWindowsDisplayVersion()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                var displayVersion = key?.GetValue("DisplayVersion")?.ToString();
                if (!string.IsNullOrEmpty(displayVersion))
                {
                    return displayVersion;
                }

                // 旧版本使用 ReleaseId
                var releaseId = key?.GetValue("ReleaseId")?.ToString();
                if (!string.IsNullOrEmpty(releaseId))
                {
                    return releaseId;
                }
            }
            catch
            {
                // 忽略错误
            }
            return null;
        }

        /// <summary>
        /// 获取 Linux 发行版名称
        /// </summary>
        private string? GetLinuxDistributionName()
        {
            try
            {
                if (File.Exists("/etc/os-release"))
                {
                    var lines = File.ReadAllLines("/etc/os-release");
                    var prettyNameLine = lines.FirstOrDefault(l => l.StartsWith("PRETTY_NAME="));
                    if (prettyNameLine != null)
                    {
                        var value = prettyNameLine.Substring("PRETTY_NAME=".Length).Trim('"');
                        return value;
                    }

                    var nameLine = lines.FirstOrDefault(l => l.StartsWith("NAME="));
                    var versionLine = lines.FirstOrDefault(l => l.StartsWith("VERSION_ID="));

                    var name = nameLine?.Substring("NAME=".Length).Trim('"');
                    var version = versionLine?.Substring("VERSION_ID=".Length).Trim('"');

                    if (!string.IsNullOrEmpty(name))
                    {
                        return string.IsNullOrEmpty(version) ? name : $"{name} {version}";
                    }
                }
            }
            catch
            {
                // 忽略错误
            }
            return null;
        }

        /// <summary>
        /// 获取内核版本
        /// </summary>
        private string GetKernelVersion()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Environment.OSVersion.Version.ToString();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // 尝试读取 /proc/version
                    if (File.Exists("/proc/version"))
                    {
                        var version = File.ReadAllText("/proc/version");
                        // 提取版本号部分
                        var parts = version.Split(' ');
                        if (parts.Length >= 3)
                        {
                            return parts[2];
                        }
                    }
                    return Environment.OSVersion.Version.ToString();
                }
                else
                {
                    return Environment.OSVersion.Version.ToString();
                }
            }
            catch
            {
                return "未知";
            }
        }

        /// <summary>
        /// 格式化字节大小
        /// </summary>
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 打开 Micro Plugin 仓库
        /// </summary>
        private void OnOpenMicroPluginRepo(object? sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/V2233/micro-plugin");
        }

        /// <summary>
        /// 打开 Micro Panel 仓库
        /// </summary>
        private void OnOpenMicroPanelRepo(object? sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/tiancra/MicroPanel");
        }

        /// <summary>
        /// 使用系统默认浏览器打开 URL
        /// </summary>
        private void OpenUrl(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"打开链接失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 版本号点击事件 - 连续点击5次进入调试模式（仅桌面模式可用）
        /// </summary>
        private async void OnVersionClick(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            // 非桌面模式下无法通过点击版本号进入调试模式
            if (!Services.DesktopModeManager.Instance.IsDesktopMode)
            {
                return;
            }

            var now = DateTime.Now;

            // 如果超过3秒，重置计数
            if ((now - _firstClickTime).TotalSeconds > ClickTimeoutSeconds)
            {
                _versionClickCount = 0;
                _firstClickTime = now;
            }

            _versionClickCount++;

            // 显示点击反馈
            if (_versionClickCount < RequiredClicks)
            {
                var remaining = RequiredClicks - _versionClickCount;
                Services.DebugModeService.LogDebug($"版本号点击 {_versionClickCount}/{RequiredClicks}，还需 {remaining} 次进入调试模式");
            }

            // 达到5次点击
            if (_versionClickCount >= RequiredClicks)
            {
                _versionClickCount = 0;
                Services.DebugModeService.LogDebug("版本号已点击5次，触发调试模式确认");

                // 获取父窗口
                var parentWindow = TopLevel.GetTopLevel(this) as Window;
                if (parentWindow != null)
                {
                    var result = await Services.DebugModeService.ShowDebugModeConfirmDialog(parentWindow);
                    if (result)
                    {
                        Services.DebugModeService.RestartInDebugMode();
                    }
                }
            }
        }
    }
}
