using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Text.RegularExpressions;

namespace MicroPanel.Views.Controls
{
    public partial class LogViewer : UserControl
    {
        private ScrollViewer? _scrollViewer;
        private StackPanel? _logStackPanel;
        private const int MaxLogLines = 1000; // 最大日志行数

        public LogViewer()
        {
            InitializeComponent();
            
            // 监听主题变化
            Application.Current!.ActualThemeVariantChanged += OnThemeChanged;
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            // 主题变化时不需要重新渲染已有日志，新日志会使用正确的颜色
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _scrollViewer = this.FindControl<ScrollViewer>("ScrollViewer");
            _logStackPanel = this.FindControl<StackPanel>("LogStackPanel");
        }

        /// <summary>
        /// 判断当前是否为深色模式
        /// </summary>
        private bool IsDarkMode => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

        /// <summary>
        /// 追加日志行
        /// </summary>
        public void AppendLog(string text)
        {
            if (_logStackPanel == null) return;

            // 删除末尾的空白行
            text = text.TrimEnd('\r', '\n', ' ', '\t');
            
            // 如果文本为空，则不添加
            if (string.IsNullOrEmpty(text)) return;

            // 解析日志行
            var logLine = ParseLogLine(text);
            
            // 创建日志行控件
            var logItem = CreateLogItem(logLine);
            
            _logStackPanel.Children.Add(logItem);

            // 限制行数
            while (_logStackPanel.Children.Count > MaxLogLines)
            {
                _logStackPanel.Children.RemoveAt(0);
            }

            // 自动滚动到底部 - 确保最后一行始终可见
            ScrollToBottom();
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        public void Clear()
        {
            _logStackPanel?.Children.Clear();
        }

        /// <summary>
        /// 获取所有日志文本
        /// </summary>
        public string GetAllLogs()
        {
            if (_logStackPanel == null) return "";

            var logs = new System.Text.StringBuilder();
            foreach (var child in _logStackPanel.Children)
            {
                if (child is StackPanel panel)
                {
                    // 从StackPanel中提取文本
                    var lineText = new System.Text.StringBuilder();
                    foreach (var element in panel.Children)
                    {
                        if (element is Border border && border.Child is TextBlock tb)
                        {
                            // 标签内容（如 [INFO]）
                            lineText.Append($"[{tb.Text}]");
                        }
                        else if (element is TextBlock textBlock)
                        {
                            // 普通文本
                            lineText.Append(textBlock.Text);
                        }
                    }
                    logs.AppendLine(lineText.ToString());
                }
            }
            return logs.ToString();
        }

        /// <summary>
        /// 设置日志内容（用于从外部加载日志）
        /// </summary>
        public void SetLogs(string logs)
        {
            Clear();
            if (string.IsNullOrEmpty(logs)) return;

            var lines = logs.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                AppendLog(line);
            }
        }

        /// <summary>
        /// 滚动到底部，确保最后一行始终可见
        /// </summary>
        private void ScrollToBottom()
        {
            if (_scrollViewer == null || _logStackPanel == null) return;

            // 使用多个延迟级别确保布局完成后再滚动
            Dispatcher.UIThread.Post(() =>
            {
                // 测量 StackPanel 的总高度
                var totalHeight = _logStackPanel.Bounds.Height;
                var viewportHeight = _scrollViewer.Bounds.Height;
                
                // 计算需要滚动的位置
                var scrollableHeight = totalHeight - viewportHeight;
                if (scrollableHeight > 0)
                {
                    _scrollViewer.Offset = new Vector(0, scrollableHeight);
                }
                
                // 再次延迟确保滚动生效
                Dispatcher.UIThread.Post(() =>
                {
                    totalHeight = _logStackPanel.Bounds.Height;
                    viewportHeight = _scrollViewer.Bounds.Height;
                    scrollableHeight = totalHeight - viewportHeight;
                    
                    if (scrollableHeight > 0)
                    {
                        _scrollViewer.Offset = new Vector(0, scrollableHeight);
                    }
                }, DispatcherPriority.Render);
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// 解析日志行，提取带颜色的标记和文本
        /// </summary>
        private List<LogSegment> ParseLogLine(string text)
        {
            var segments = new List<LogSegment>();
            
            // ANSI 颜色代码正则表达式
            var ansiPattern = @"\u001b\[([0-9;]*)m";
            
            var matches = Regex.Matches(text, ansiPattern);
            int lastIndex = 0;
            // 根据主题选择默认颜色：深色模式用白色，浅色模式用黑色
            IBrush currentColor = IsDarkMode ? Brushes.White : Brushes.Black;
            bool isBold = false;

            foreach (Match match in matches)
            {
                // 添加匹配前的文本
                if (match.Index > lastIndex)
                {
                    var content = text.Substring(lastIndex, match.Index - lastIndex);
                    // 替换简写为完整单词
                    content = ReplaceAbbreviations(content);
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        segments.Add(new LogSegment
                        {
                            Content = content,
                            Foreground = currentColor,
                            IsBold = isBold,
                            IsTag = content.StartsWith("[") && content.Contains("]")
                        });
                    }
                }

                // 解析 ANSI 代码
                var codes = match.Groups[1].Value.Split(';');
                foreach (var code in codes)
                {
                    if (string.IsNullOrEmpty(code)) continue;
                    
                    var color = ParseAnsiCode(code, ref isBold);
                    if (color != null)
                    {
                        currentColor = color;
                    }
                }

                lastIndex = match.Index + match.Length;
            }

            // 添加剩余文本
                if (lastIndex < text.Length)
                {
                    var content = text.Substring(lastIndex);
                    content = ReplaceAbbreviations(content);
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        // 进一步拆分内容，识别独立的标记
                        var subSegments = SplitContentIntoSegments(content, currentColor, isBold);
                        segments.AddRange(subSegments);
                    }
                }

                return segments;
        }

        /// <summary>
        /// 将内容拆分为独立的片段，只识别时间戳、日志级别、QQ号三种标记
        /// </summary>
        private List<LogSegment> SplitContentIntoSegments(string content, IBrush color, bool isBold)
        {
            var segments = new List<LogSegment>();
            
            // 只匹配三种特定格式：
            // 1. 时间戳: [HH:mm:ss.fff] 或 [HH:mm:ss]
            // 2. 日志级别: [INFO], [WARN], [ERROR], [DEBUG], [TRACE], [MARK]
            // 3. QQ号/群号信息: [数字 <= 数字, 数字] 或 [数字 => 数字, 数字] 或 [数字]
            // 支持中英文逗号和逗号后的空格
            var tagPattern = @"\[(\d{2}:\d{2}:\d{2}(\.\d{3})?|INFO|WARN|ERROR|DEBUG|TRACE|MARK|\d+\s*(<=|=>)\s*[\d,，](\s*[\d,，])*|\d{6,})\]";
            var matches = Regex.Matches(content, tagPattern);
            
            int lastIndex = 0;
            foreach (Match match in matches)
            {
                // 添加标记前的文本
                if (match.Index > lastIndex)
                {
                    var textBefore = content.Substring(lastIndex, match.Index - lastIndex);
                    if (!string.IsNullOrEmpty(textBefore))
                    {
                        segments.Add(new LogSegment
                        {
                            Content = textBefore,
                            Foreground = color,
                            IsBold = isBold,
                            IsTag = false
                        });
                    }
                }
                
                // 添加标记（只有这三种会被标记为 IsTag=true）
                segments.Add(new LogSegment
                {
                    Content = match.Value,
                    Foreground = color,
                    IsBold = isBold,
                    IsTag = true
                });
                
                lastIndex = match.Index + match.Length;
            }
            
            // 添加剩余文本
            if (lastIndex < content.Length)
            {
                var textAfter = content.Substring(lastIndex);
                if (!string.IsNullOrEmpty(textAfter))
                {
                    segments.Add(new LogSegment
                    {
                        Content = textAfter,
                        Foreground = color,
                        IsBold = isBold,
                        IsTag = false
                    });
                }
            }
            
            return segments;
        }

        /// <summary>
        /// 替换简写为完整单词
        /// </summary>
        private string ReplaceAbbreviations(string text)
        {
            // 替换常见的日志级别简写
            text = text.Replace("[INFO]", "[INFO]");
            text = text.Replace("[WARN]", "[WARN]");
            text = text.Replace("[ERRO]", "[ERROR]");
            text = text.Replace("[MARK]", "[MARK]");
            text = text.Replace("[DEBU]", "[DEBUG]");
            text = text.Replace("[TRAC]", "[TRACE]");
            
            return text;
        }

        /// <summary>
        /// 解析 ANSI 颜色代码（根据主题调整）
        /// </summary>
        private IBrush? ParseAnsiCode(string code, ref bool isBold)
        {
            // 浅色模式：黑白反转
            // 深色模式：保持原样
            if (!IsDarkMode)
            {
                // 浅色模式 - 黑白反转
                return code switch
                {
                    "0" => Brushes.Black,  // 重置
                    "1" => null, // 粗体
                    "30" => Brushes.White,  // 黑色 -> 白色
                    "31" => Brushes.Red,
                    "32" => Brushes.Green,
                    "33" => Brushes.Yellow,
                    "34" => Brushes.Blue,
                    "35" => Brushes.Magenta,
                    "36" => Brushes.Cyan,
                    "37" => Brushes.Black,  // 白色 -> 黑色
                    "90" => Brushes.LightGray,
                    "91" => Brushes.LightCoral,
                    "92" => Brushes.LightGreen,
                    "93" => Brushes.LightYellow,
                    "94" => Brushes.LightBlue,
                    "95" => Brushes.LightPink,
                    "96" => Brushes.LightCyan,
                    "97" => Brushes.Black,  // 亮白 -> 黑色
                    _ => null
                };
            }
            else
            {
                // 深色模式 - 保持原样
                return code switch
                {
                    "0" => Brushes.White,  // 重置
                    "1" => null, // 粗体
                    "30" => Brushes.Black,
                    "31" => Brushes.Red,
                    "32" => Brushes.Green,
                    "33" => Brushes.Yellow,
                    "34" => Brushes.Blue,
                    "35" => Brushes.Magenta,
                    "36" => Brushes.Cyan,
                    "37" => Brushes.White,
                    "90" => Brushes.Gray,
                    "91" => Brushes.LightCoral,
                    "92" => Brushes.LightGreen,
                    "93" => Brushes.LightYellow,
                    "94" => Brushes.LightBlue,
                    "95" => Brushes.LightPink,
                    "96" => Brushes.LightCyan,
                    "97" => Brushes.White,
                    _ => null
                };
            }
        }

        /// <summary>
        /// 创建日志行控件
        /// </summary>
        private Control CreateLogItem(List<LogSegment> segments)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Margin = new Thickness(0, 2, 0, 2)
            };

            // 遍历所有片段，每个标记都单独创建椭圆
            foreach (var segment in segments)
            {
                if (segment.IsTag && segment.Content.StartsWith("[") && segment.Content.EndsWith("]"))
                {
                    // 为每个标记创建椭圆边框，根据内容设置颜色
                    var tagContent = segment.Content.Trim('[', ']');
                    var border = new Border
                    {
                        Background = GetTagBackground(tagContent),
                        CornerRadius = new CornerRadius(12),
                        Padding = new Thickness(8, 2, 8, 2),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = tagContent,
                            Foreground = Brushes.White,
                            FontSize = 11,
                            FontWeight = FontWeight.Bold,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                    panel.Children.Add(border);
                }
                else
                {
                    // 普通文本
                    if (!string.IsNullOrEmpty(segment.Content))
                    {
                        var textBlock = new TextBlock
                        {
                            Text = segment.Content,
                            Foreground = segment.Foreground,
                            FontFamily = new FontFamily("Consolas, Monaco, 'Courier New', monospace"),
                            FontSize = 13,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextWrapping = TextWrapping.NoWrap
                        };
                        panel.Children.Add(textBlock);
                    }
                }
            }

            return panel;
        }

        /// <summary>
        /// 根据前景色获取对应的背景色（用于椭圆）
        /// </summary>
        private IBrush GetBrushFromForeground(IBrush foreground)
        {
            // 根据前景色返回对应的背景色
            if (foreground is SolidColorBrush solidBrush)
            {
                var color = solidBrush.Color;
                
                // 根据颜色返回对应的深色背景
                return color.ToString() switch
                {
                    var c when c.Contains("Blue") => new SolidColorBrush(Color.Parse("#1976D2")),
                    var c when c.Contains("Green") => new SolidColorBrush(Color.Parse("#388E3C")),
                    var c when c.Contains("Red") => new SolidColorBrush(Color.Parse("#D32F2F")),
                    var c when c.Contains("Yellow") => new SolidColorBrush(Color.Parse("#FBC02D")),
                    var c when c.Contains("Cyan") => new SolidColorBrush(Color.Parse("#0097A7")),
                    var c when c.Contains("Magenta") => new SolidColorBrush(Color.Parse("#7B1FA2")),
                    var c when c.Contains("Black") => new SolidColorBrush(Color.Parse("#424242")),
                    var c when c.Contains("White") => new SolidColorBrush(Color.Parse("#757575")),
                    _ => new SolidColorBrush(Color.Parse("#616161"))
                };
            }
            
            return new SolidColorBrush(Color.Parse("#616161"));
        }

        /// <summary>
        /// 根据标记内容获取背景色
        /// </summary>
        private IBrush GetTagBackground(string tag)
        {
            var upperTag = tag.ToUpper();
            
            // 日志级别颜色
            if (upperTag == "INFO" || upperTag == "INF0")
                return new SolidColorBrush(Color.Parse("#4CAF50")); // 绿色
            
            if (upperTag == "MARK")
                return new SolidColorBrush(Color.Parse("#9E9E9E")); // 灰色
            
            if (upperTag == "ERROR" || upperTag == "ERRO")
                return new SolidColorBrush(Color.Parse("#F44336")); // 红色
            
            if (upperTag == "WARN")
                return new SolidColorBrush(Color.Parse("#FF9800")); // 橙色
            
            if (upperTag == "DEBUG" || upperTag == "DEBU")
                return new SolidColorBrush(Color.Parse("#9C27B0")); // 紫色
            
            if (upperTag == "TRACE" || upperTag == "TRCE")
                return new SolidColorBrush(Color.Parse("#607D8B")); // 蓝灰色
            
            // 时间戳 - 蓝色
            if (Regex.IsMatch(tag, @"^\d{2}:\d{2}:\d{2}"))
                return new SolidColorBrush(Color.Parse("#2196F3")); // 蓝色
            
            // QQ号/群号信息 - 青色（包含 <= 或 =>）
            if (Regex.IsMatch(tag, @"^\d+\s*<=|^\d+\s*=>") || Regex.IsMatch(tag, @"^\d{6,}$"))
                return new SolidColorBrush(Color.Parse("#00BCD4")); // 青色
            
            return new SolidColorBrush(Color.Parse("#757575")); // 默认灰色
        }
    }

    /// <summary>
    /// 日志片段
    /// </summary>
    public class LogSegment
    {
        public string Content { get; set; } = "";
        public IBrush Foreground { get; set; } = Brushes.White;
        public bool IsBold { get; set; }
        public bool IsTag { get; set; }
    }
}
