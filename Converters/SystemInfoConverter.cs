using Avalonia.Data.Converters;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MicroPanel.Converters
{
    public class CpuUsageConverter : IValueConverter
    {
        public static readonly CpuUsageConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string cpuInfo && !string.IsNullOrEmpty(cpuInfo))
            {
                // 尝试从字符串中提取 CPU 使用率
                // 常见的格式: "15%" 或 "CPU: 15%" 或 "Intel(R) Core(TM) i7-9700K @ 3.60GHz (15%)"
                var match = Regex.Match(cpuInfo, @"(\d+(?:\.\d+)?)\s*%");
                if (match.Success)
                {
                    return $"{match.Groups[1].Value}%";
                }
                // 如果无法提取百分比，返回前 20 个字符
                return cpuInfo.Length > 20 ? cpuInfo.Substring(0, 20) + "..." : cpuInfo;
            }
            return "--";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RamUsageConverter : IValueConverter
    {
        public static readonly RamUsageConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string ramInfo && !string.IsNullOrEmpty(ramInfo))
            {
                // 尝试从字符串中提取内存使用率或已用/总量
                // 常见的格式: "45%" 或 "4.5GB / 16GB" 或 "Used: 4.5GB, Total: 16GB"
                
                // 先尝试匹配百分比
                var percentMatch = Regex.Match(ramInfo, @"(\d+(?:\.\d+)?)\s*%");
                if (percentMatch.Success)
                {
                    return $"{percentMatch.Groups[1].Value}%";
                }
                
                // 尝试匹配 GB/MB 格式
                var gbMatch = Regex.Match(ramInfo, @"(\d+(?:\.\d+)?)\s*(GB|MB)\s*/\s*(\d+(?:\.\d+)?)\s*(GB|MB)");
                if (gbMatch.Success)
                {
                    return $"{gbMatch.Groups[1].Value}{gbMatch.Groups[2].Value} / {gbMatch.Groups[3].Value}{gbMatch.Groups[4].Value}";
                }
                
                // 返回前 15 个字符
                return ramInfo.Length > 15 ? ramInfo.Substring(0, 15) + "..." : ramInfo;
            }
            return "--";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DiskUsageConverter : IValueConverter
    {
        public static readonly DiskUsageConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string diskInfo && !string.IsNullOrEmpty(diskInfo))
            {
                // 尝试从字符串中提取磁盘使用率
                // 常见的格式: "C: 50GB / 100GB (50%)" 或 "50%" 或 "50GB / 100GB"
                
                // 尝试匹配完整的磁盘信息
                var fullMatch = Regex.Match(diskInfo, @"([A-Z]:)?\s*(\d+(?:\.\d+)?)\s*(GB|MB|TB)\s*/\s*(\d+(?:\.\d+)?)\s*(GB|MB|TB)");
                if (fullMatch.Success)
                {
                    var drive = fullMatch.Groups[1].Value;
                    var used = fullMatch.Groups[2].Value;
                    var usedUnit = fullMatch.Groups[3].Value;
                    var total = fullMatch.Groups[4].Value;
                    var totalUnit = fullMatch.Groups[5].Value;
                    
                    if (!string.IsNullOrEmpty(drive))
                    {
                        return $"{drive} {used}{usedUnit} / {total}{totalUnit}";
                    }
                    return $"{used}{usedUnit} / {total}{totalUnit}";
                }
                
                // 尝试匹配百分比
                var percentMatch = Regex.Match(diskInfo, @"(\d+(?:\.\d+)?)\s*%");
                if (percentMatch.Success)
                {
                    return $"{percentMatch.Groups[1].Value}%";
                }
                
                // 返回前 25 个字符
                return diskInfo.Length > 25 ? diskInfo.Substring(0, 25) + "..." : diskInfo;
            }
            return "--";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
