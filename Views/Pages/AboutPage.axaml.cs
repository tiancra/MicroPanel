using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MicroPanelAvalonia.Views.Pages
{
    public partial class AboutPage : UserControl
    {
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
        /// 获取 CPU 信息
        /// </summary>
        private string GetCpuInfo()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var processorCount = Environment.ProcessorCount;
                    return $"{processorCount} 核处理器";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var processorCount = Environment.ProcessorCount;
                    return $"{processorCount} 核处理器";
                }
                else
                {
                    return $"{Environment.ProcessorCount} 核处理器";
                }
            }
            catch
            {
                return "未知";
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
        /// 获取操作系统版本
        /// </summary>
        private string GetOsVersion()
        {
            try
            {
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
    }
}
