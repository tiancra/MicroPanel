using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MicroPanel.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace MicroPanel.Views.Pages
{
    public partial class StatusPage : UserControl
    {
        private readonly AuthenticatedApiService _apiService;
        private Timer? _refreshTimer;
        private bool _isAutoRefresh = true;

        public StatusPage()
        {
            InitializeComponent();
            _apiService = new AuthenticatedApiService();

            // 页面加载时初始化
            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnPageLoaded(object? sender, RoutedEventArgs e)
        {
            // 绑定控件事件
            var refreshButton = this.FindControl<Button>("RefreshButton");
            var autoRefreshToggle = this.FindControl<ToggleSwitch>("AutoRefreshToggle");

            if (refreshButton != null)
                refreshButton.Click += OnRefreshClick;

            if (autoRefreshToggle != null)
            {
                autoRefreshToggle.IsCheckedChanged += OnAutoRefreshChanged;
                _isAutoRefresh = autoRefreshToggle.IsChecked ?? true;
            }

            // 设置API基础URL
            var session = SessionService.Instance;
            if (session.IsLoggedIn)
            {
                _apiService.SetBaseUrl(session.CurrentServer?.ServerAddress ?? "");
            }

            // 根据自动刷新设置决定行为
            if (_isAutoRefresh)
            {
                // 自动刷新开启：立即加载数据并启动定时器
                _ = LoadDataAsync();
                StartRefreshTimer();
            }
            else
            {
                // 自动刷新关闭：只加载一次数据
                _ = LoadDataAsync();
            }
        }

        private void OnPageUnloaded(object? sender, RoutedEventArgs e)
        {
            StopRefreshTimer();
        }

        private void StartRefreshTimer()
        {
            StopRefreshTimer();
            
            // 只有在自动刷新开启时才启动定时器
            if (!_isAutoRefresh) return;
            
            _refreshTimer = new Timer(5000); // 5秒刷新一次
            _refreshTimer.Elapsed += async (s, e) =>
            {
                // 每次触发时检查自动刷新开关
                if (_isAutoRefresh)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await LoadDataAsync();
                    });
                }
            };
            _refreshTimer.AutoReset = true;
            _refreshTimer.Start();
            System.Diagnostics.Debug.WriteLine("StatusPage: 启动定时刷新");
        }

        private void StopRefreshTimer()
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _refreshTimer = null;
        }

        private void OnRefreshClick(object? sender, RoutedEventArgs e)
        {
            _ = LoadDataAsync();
        }

        private void OnAutoRefreshChanged(object? sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggle)
            {
                _isAutoRefresh = toggle.IsChecked ?? true;
                
                if (_isAutoRefresh)
                {
                    // 开启自动刷新：启动定时器
                    StartRefreshTimer();
                }
                else
                {
                    // 关闭自动刷新：停止定时器
                    StopRefreshTimer();
                }
            }
        }

        private async Task LoadDataAsync()
        {
            var session = SessionService.Instance;
            if (!session.IsLoggedIn) 
            {
                System.Diagnostics.Debug.WriteLine("StatusPage: 用户未登录");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("StatusPage: 开始加载系统状态");
                var response = await _apiService.GetSystemStatusAsync(session.Token!);
                System.Diagnostics.Debug.WriteLine($"StatusPage: 响应 Code={response?.Code}, Message={response?.Message}");
                
                if (response?.IsSuccess == true && response.Data != null)
                {
                    System.Diagnostics.Debug.WriteLine($"StatusPage: 数据不为空，开始更新UI");
                    UpdateUI(response.Data);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"StatusPage: 获取数据失败 - {response?.Message}");
                }

                // 更新时间显示
                var lastUpdateText = this.FindControl<TextBlock>("LastUpdateText");
                if (lastUpdateText != null)
                {
                    lastUpdateText.Text = $"上次更新: {DateTime.Now:HH:mm:ss}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StatusPage: 加载系统状态失败 - {ex.Message}");
            }
        }

        private void UpdateUI(SystemStatusData data)
        {
            // 更新CPU
            UpdateCpuInfo(data.CpuInfo);

            // 更新内存
            UpdateRamInfo(data.RamInfo);

            // 更新GPU
            UpdateGpuInfo(data.GpuInfo);

            // 更新磁盘
            UpdateDiskInfo(data.DiskSizeInfo, data.SwapInfo);

            // 更新Node.js信息
            UpdateNodeInfo(data.NodeInfo);

            // 更新其它信息
            UpdateOtherInfo(data.OtherInfo);

            // 更新网络信息
            UpdateNetworkInfo(data.NetworkInfo);
        }

        private void UpdateCpuInfo(CpuInfo? cpuInfo)
        {
            var progressBar = this.FindControl<Controls.CircularProgressBar>("CpuProgressBar");
            var percentText = this.FindControl<TextBlock>("CpuPercentText");
            var infoText = this.FindControl<TextBlock>("CpuInfoText");

            if (cpuInfo == null) return;

            var percent = (int)(cpuInfo.Inner * 100);

            if (progressBar != null)
            {
                progressBar.Value = percent;
                // 根据使用率改变颜色
                if (percent < 60)
                    progressBar.ForegroundBrush = new SolidColorBrush(Colors.DodgerBlue);
                else if (percent < 80)
                    progressBar.ForegroundBrush = new SolidColorBrush(Colors.Orange);
                else
                    progressBar.ForegroundBrush = new SolidColorBrush(Colors.Red);
            }

            if (percentText != null)
                percentText.Text = $"{percent}%";

            if (infoText != null && cpuInfo.Info?.Length >= 3)
            {
                infoText.Text = $"{cpuInfo.Info[0]} {cpuInfo.Info[1]}\n{cpuInfo.Info[2]}";
            }
        }

        private void UpdateRamInfo(RamInfo? ramInfo)
        {
            var progressBar = this.FindControl<Controls.CircularProgressBar>("RamProgressBar");
            var percentText = this.FindControl<TextBlock>("RamPercentText");
            var infoText = this.FindControl<TextBlock>("RamInfoText");

            if (ramInfo == null) return;

            var percentStr = ramInfo.Inner?.Replace("%", "") ?? "0";
            if (double.TryParse(percentStr, out var percentValue))
            {
                var percent = (int)percentValue;

                if (progressBar != null)
                {
                    progressBar.Value = percent;
                    if (percent < 60)
                        progressBar.ForegroundBrush = new SolidColorBrush(Colors.Green);
                    else if (percent < 80)
                        progressBar.ForegroundBrush = new SolidColorBrush(Colors.Orange);
                    else
                        progressBar.ForegroundBrush = new SolidColorBrush(Colors.Red);
                }

                if (percentText != null)
                    percentText.Text = $"{percent}%";
            }

            if (infoText != null && ramInfo.Info?.Length > 0)
            {
                infoText.Text = ramInfo.Info[0];
            }
        }

        private void UpdateGpuInfo(object? gpuInfoObj)
        {
            var progressBar = this.FindControl<Controls.CircularProgressBar>("GpuProgressBar");
            var percentText = this.FindControl<TextBlock>("GpuPercentText");
            var infoText = this.FindControl<TextBlock>("GpuInfoText");

            // 检查 gpuInfo 是否为 false 或 null
            if (gpuInfoObj == null || gpuInfoObj is bool)
            {
                if (percentText != null) percentText.Text = "--";
                if (infoText != null) infoText.Text = "未检测到GPU";
                if (progressBar != null) progressBar.IsVisible = false;
                return;
            }

            // 尝试转换为 GpuInfo
            GpuInfo? gpuInfo = null;
            try
            {
                gpuInfo = System.Text.Json.JsonSerializer.Deserialize<GpuInfo>(
                    System.Text.Json.JsonSerializer.Serialize(gpuInfoObj));
            }
            catch
            {
                if (percentText != null) percentText.Text = "--";
                if (infoText != null) infoText.Text = "未检测到GPU";
                if (progressBar != null) progressBar.IsVisible = false;
                return;
            }

            if (progressBar != null) progressBar.IsVisible = true;

            var percent = (int)(gpuInfo.Inner * 100);

            if (progressBar != null)
            {
                progressBar.Value = percent;
                if (percent < 60)
                    progressBar.ForegroundBrush = new SolidColorBrush(Colors.Goldenrod);
                else if (percent < 80)
                    progressBar.ForegroundBrush = new SolidColorBrush(Colors.Orange);
                else
                    progressBar.ForegroundBrush = new SolidColorBrush(Colors.Red);
            }

            if (percentText != null)
                percentText.Text = $"{percent}%";

            if (infoText != null && gpuInfo.Info?.Length >= 2)
            {
                infoText.Text = $"{gpuInfo.Info[0]}\n{gpuInfo.Info[1]}";
            }
        }

        private void UpdateDiskInfo(List<DiskInfo>? diskInfos, SwapInfo? swapInfo)
        {
            var stackPanel = this.FindControl<StackPanel>("DiskStackPanel");
            if (stackPanel == null) return;

            stackPanel.Children.Clear();

            // 添加磁盘信息
            if (diskInfos != null)
            {
                foreach (var disk in diskInfos)
                {
                    var diskItem = CreateDiskItem(disk);
                    stackPanel.Children.Add(diskItem);
                }
            }

            // 添加交换分区信息
            if (swapInfo != null)
            {
                var swapItem = CreateSwapItem(swapInfo);
                stackPanel.Children.Add(swapItem);
            }
        }

        private Control CreateDiskItem(DiskInfo disk)
        {
            var percent = (int)(disk.Percentage * 100);
            var brush = GetStatusBrush(percent);

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("100,*,Auto")
            };

            var fsText = new TextBlock
            {
                Text = disk.Fs,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontWeight = FontWeight.SemiBold
            };
            Grid.SetColumn(fsText, 0);

            var progressBar = new ProgressBar
            {
                Value = percent,
                Maximum = 100,
                Height = 20,
                Margin = new Thickness(12, 0),
                Foreground = brush,
                Background = new SolidColorBrush(Colors.Gray) { Opacity = 0.2 }
            };
            Grid.SetColumn(progressBar, 1);

            var infoText = new TextBlock
            {
                Text = $"{disk.Used} / {disk.Size} ({percent}%)",
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Opacity = 0.8,
                FontSize = 12
            };
            Grid.SetColumn(infoText, 2);

            grid.Children.Add(fsText);
            grid.Children.Add(progressBar);
            grid.Children.Add(infoText);

            return grid;
        }

        private Control CreateSwapItem(SwapInfo swap)
        {
            var percentStr = swap.Inner?.Replace("%", "") ?? "0";
            int.TryParse(percentStr, out var percent);
            var brush = GetStatusBrush(percent);

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("100,*,Auto")
            };

            var titleText = new TextBlock
            {
                Text = $"{swap.Title}:",
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontWeight = FontWeight.SemiBold
            };
            Grid.SetColumn(titleText, 0);

            var progressBar = new ProgressBar
            {
                Value = percent,
                Maximum = 100,
                Height = 20,
                Margin = new Thickness(12, 0),
                Foreground = brush,
                Background = new SolidColorBrush(Colors.Gray) { Opacity = 0.2 }
            };
            Grid.SetColumn(progressBar, 1);

            var infoText = new TextBlock
            {
                Text = $"{swap.Info?[0]} / {swap.Info?[1]} ({percent}%)",
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Opacity = 0.8,
                FontSize = 12
            };
            Grid.SetColumn(infoText, 2);

            grid.Children.Add(titleText);
            grid.Children.Add(progressBar);
            grid.Children.Add(infoText);

            return grid;
        }

        private void UpdateNodeInfo(NodeInfo? nodeInfo)
        {
            var rssText = this.FindControl<TextBlock>("NodeRssText");
            var heapTotalText = this.FindControl<TextBlock>("NodeHeapTotalText");
            var heapUsedText = this.FindControl<TextBlock>("NodeHeapUsedText");
            var progressBar = this.FindControl<ProgressBar>("NodeHeapProgressBar");

            if (nodeInfo?.Info == null)
            {
                if (rssText != null) rssText.Text = "0 MB";
                if (heapTotalText != null) heapTotalText.Text = "0 MB";
                if (heapUsedText != null) heapUsedText.Text = "0 MB";
                if (progressBar != null) progressBar.Value = 0;
                return;
            }

            if (rssText != null)
                rssText.Text = nodeInfo.Info.Rss ?? "0 MB";

            if (heapTotalText != null)
                heapTotalText.Text = nodeInfo.Info.HeapTotal ?? "0 MB";

            if (heapUsedText != null)
                heapUsedText.Text = nodeInfo.Info.HeapUsed ?? "0 MB";

            if (progressBar != null && nodeInfo.Info.Occupy > 0)
            {
                progressBar.Value = nodeInfo.Info.Occupy * 100;
            }
        }

        private void UpdateOtherInfo(List<OtherInfoItem>? otherInfos)
        {
            var stackPanel = this.FindControl<StackPanel>("OtherInfoStackPanel");
            if (stackPanel == null) return;

            stackPanel.Children.Clear();

            if (otherInfos == null) return;

            foreach (var item in otherInfos)
            {
                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("150,*")
                };

                var titleText = new TextBlock
                {
                    Text = item.First + "：",
                    FontWeight = FontWeight.SemiBold,
                    Opacity = 0.8
                };
                Grid.SetColumn(titleText, 0);

                Control detailControl;
                if (item.First == "环境版本" && item.Tail is JsonElement envElement)
                {
                    // 处理环境版本对象
                    var envText = new TextBlock();
                    try
                    {
                        var node = envElement.GetProperty("node").GetString();
                        var git = envElement.GetProperty("git").GetString();
                        envText.Text = $"node: {node} / git: {git}";
                    }
                    catch
                    {
                        envText.Text = item.Tail?.ToString() ?? "--";
                    }
                    detailControl = envText;
                }
                else
                {
                    detailControl = new TextBlock
                    {
                        Text = item.Tail?.ToString() ?? "--",
                        TextWrapping = TextWrapping.Wrap
                    };
                }
                Grid.SetColumn(detailControl, 1);

                grid.Children.Add(titleText);
                grid.Children.Add(detailControl);
                stackPanel.Children.Add(grid);
            }
        }

        private void UpdateNetworkInfo(List<NetworkInfo>? networkInfos)
        {
            var stackPanel = this.FindControl<StackPanel>("NetworkStackPanel");
            if (stackPanel == null) return;

            stackPanel.Children.Clear();

            if (networkInfos == null || networkInfos.Count == 0)
            {
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "暂无网络信息",
                    Opacity = 0.5,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                });
                return;
            }

            foreach (var net in networkInfos)
            {
                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("150,*,*")
                };

                var ifaceText = new TextBlock
                {
                    Text = net.Iface,
                    FontWeight = FontWeight.SemiBold,
                    Opacity = 0.8
                };
                Grid.SetColumn(ifaceText, 0);

                var rxText = new TextBlock
                {
                    Text = $"接收: {net.RxBytes}",
                    Foreground = new SolidColorBrush(Colors.Green)
                };
                Grid.SetColumn(rxText, 1);

                var txText = new TextBlock
                {
                    Text = $"发送: {net.TxBytes}",
                    Foreground = new SolidColorBrush(Colors.DodgerBlue)
                };
                Grid.SetColumn(txText, 2);

                grid.Children.Add(ifaceText);
                grid.Children.Add(rxText);
                grid.Children.Add(txText);
                stackPanel.Children.Add(grid);
            }
        }

        private IBrush GetStatusBrush(int percent)
        {
            if (percent < 60)
                return new SolidColorBrush(Colors.DodgerBlue);
            if (percent < 80)
                return new SolidColorBrush(Colors.Orange);
            return new SolidColorBrush(Colors.Red);
        }
    }
}
