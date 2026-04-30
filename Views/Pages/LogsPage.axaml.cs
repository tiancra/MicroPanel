using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MicroPanelAvalonia.Services;
using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MicroPanelAvalonia.Views.Pages
{
    public partial class LogsPage : UserControl
    {
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cts;
        private bool _isConnected = false;
        private StringBuilder _logBuffer = new StringBuilder();
        private const int MaxLogLength = 100000; // 最大日志长度，防止内存溢出
        
        // 使用静态变量，确保页面切换时独立窗口状态保持
        private static Windows.LogWindow? _logWindow;
        private bool _isInSeparateWindow = false;
        
        // 静态事件，用于通知所有日志页面实例独立窗口已关闭
        private static event EventHandler? LogWindowClosed;

        public LogsPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnPageLoaded(object? sender, EventArgs e)
        {
            // 绑定按钮事件
            var clearButton = this.FindControl<Button>("ClearButton");
            var reconnectButton = this.FindControl<Button>("ReconnectButton");
            var openWindowButton = this.FindControl<Button>("OpenWindowButton");

            if (clearButton != null)
                clearButton.Click += OnClearClick;

            if (reconnectButton != null)
                reconnectButton.Click += OnReconnectClick;

            if (openWindowButton != null)
                openWindowButton.Click += OnOpenWindowClick;

            // 订阅独立窗口关闭事件
            LogWindowClosed += OnLogWindowClosed;
            
            // 根据独立窗口状态更新页面显示
            UpdatePageVisibility();

            // 连接 WebSocket（如果独立窗口未打开）
            if (_logWindow == null)
            {
                _ = ConnectWebSocketAsync();
            }
        }

        private void OnPageUnloaded(object? sender, EventArgs e)
        {
            // 取消订阅独立窗口关闭事件
            LogWindowClosed -= OnLogWindowClosed;
            
            // 如果独立窗口未打开，才断开WebSocket
            if (_logWindow == null)
            {
                DisconnectWebSocket();
            }
            // 如果独立窗口已打开，保持连接，由独立窗口管理
        }
        
        /// <summary>
        /// 独立窗口关闭时的处理
        /// </summary>
        private void OnLogWindowClosed(object? sender, EventArgs e)
        {
            // 在UI线程上更新显示
            Dispatcher.UIThread.Post(() =>
            {
                var container = this.FindControl<Border>("LogContainer");
                if (container != null)
                {
                    container.IsVisible = true;
                }
                
                var toolbar = this.FindControl<StackPanel>("ToolbarPanel");
                if (toolbar != null)
                {
                    toolbar.IsVisible = true;
                }
                
                // 重新连接WebSocket
                _ = ConnectWebSocketAsync();
            });
        }

        /// <summary>
        /// 根据独立窗口状态更新页面可见性
        /// </summary>
        private void UpdatePageVisibility()
        {
            var logContainer = this.FindControl<Border>("LogContainer");
            var toolbarPanel = this.FindControl<StackPanel>("ToolbarPanel");

            if (_logWindow != null)
            {
                // 独立窗口已打开，清空右侧（隐藏日志容器和工具栏）
                if (logContainer != null) logContainer.IsVisible = false;
                if (toolbarPanel != null) toolbarPanel.IsVisible = false;
            }
            else
            {
                // 独立窗口未打开，显示原页面内容
                if (logContainer != null) logContainer.IsVisible = true;
                if (toolbarPanel != null) toolbarPanel.IsVisible = true;
            }
        }

        private async Task ConnectWebSocketAsync()
        {
            var session = SessionService.Instance;
            if (!session.IsLoggedIn || session.CurrentServer == null)
            {
                return;
            }

            DisconnectWebSocket();

            try
            {
                _cts = new CancellationTokenSource();
                _webSocket = new ClientWebSocket();

                // 构建 WebSocket URL
                var serverAddress = session.CurrentServer.ServerAddress;
                var wsUrl = serverAddress.Replace("http://", "ws://").Replace("https://", "wss://");
                var fullUrl = $"{wsUrl}/micro/webui/stdout";

                System.Diagnostics.Debug.WriteLine($"LogsPage: 连接WebSocket: {fullUrl}");

                await _webSocket.ConnectAsync(new Uri(fullUrl), _cts.Token);
                _isConnected = true;
                UpdateConnectionStatus();

                // 开始接收消息
                _ = ReceiveMessagesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogsPage: WebSocket连接失败: {ex.Message}");
                _isConnected = false;
                UpdateConnectionStatus();
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            if (_webSocket == null || _cts == null) return;

            var buffer = new byte[4096];

            try
            {
                while (_webSocket?.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (_webSocket?.State == WebSocketState.Open)
                        {
                            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, _cts.Token);
                        }
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Debug.WriteLine($"[LogsPage] 收到消息: {message}");

                    // 解析 JSON 消息，提取日志数据
                    try
                    {
                        var msgObj = JsonSerializer.Deserialize<WebSocketMessage>(message);
                        Debug.WriteLine($"[LogsPage] 解析结果: Type={msgObj?.Type}, DataLength={msgObj?.Data?.Length ?? 0}");
                        if (msgObj?.Type == "log" && !string.IsNullOrEmpty(msgObj.Data))
                        {
                            // 过滤掉 [micro-stdout]客户端连接！消息
                            if (msgObj.Data.Contains("[micro-stdout]客户端连接！"))
                            {
                                Debug.WriteLine($"[LogsPage] 过滤掉连接消息");
                                continue;
                            }

                            Debug.WriteLine($"[LogsPage] 调用 AppendLog，数据长度: {msgObj.Data.Length}");
                            // 去除 ANSI 转义序列
                            var cleanData = RemoveAnsiCodes(msgObj.Data);
                            AppendLog(cleanData);
                        }
                        else
                        {
                            Debug.WriteLine($"[LogsPage] 消息类型不匹配或数据为空");
                        }
                    }
                    catch (JsonException ex)
                    {
                        Debug.WriteLine($"[LogsPage] JSON 解析失败: {ex.Message}");
                        // 如果不是 JSON 格式，直接显示
                        AppendLog(RemoveAnsiCodes(message));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogsPage: 接收消息异常: {ex.Message}");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AppendLog($"[micro-stdout] 接收异常: {ex.Message}\n");
                });
            }
            finally
            {
                _isConnected = false;
                UpdateConnectionStatus();
            }
        }

        private void DisconnectWebSocket()
        {
            _cts?.Cancel();

            if (_webSocket != null)
            {
                try
                {
                    if (_webSocket.State == WebSocketState.Open)
                    {
                        _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).Wait(TimeSpan.FromSeconds(2));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LogsPage: 关闭WebSocket异常: {ex.Message}");
                }
                finally
                {
                    _webSocket.Dispose();
                    _webSocket = null;
                }
            }

            _cts?.Dispose();
            _cts = null;
            _isConnected = false;
        }

        private void AppendLog(string text)
        {
            Debug.WriteLine($"[AppendLog] 准备添加日志，长度={text.Length}");
            
            Dispatcher.UIThread.Post(() =>
            {
                var logViewer = this.FindControl<Controls.LogViewer>("LogViewer");
                if (logViewer == null) 
                {
                    Debug.WriteLine("[AppendLog] LogViewer 为 null");
                    return;
                }

                // 使用 LogViewer 追加日志
                logViewer.AppendLog(text);
                Debug.WriteLine($"[AppendLog] 日志已添加到 LogViewer");
            });
        }

        private void UpdateConnectionStatus()
        {
            var statusBorder = this.FindControl<Border>("ConnectionStatusBorder");
            var statusText = this.FindControl<TextBlock>("ConnectionStatusText");

            if (statusBorder != null && statusText != null)
            {
                if (_isConnected)
                {
                    statusBorder.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Green);
                    statusText.Text = "已连接";
                }
                else
                {
                    statusBorder.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Orange);
                    statusText.Text = "未连接";
                }
            }
        }

        private void OnClearClick(object? sender, RoutedEventArgs e)
        {
            var logViewer = this.FindControl<Controls.LogViewer>("LogViewer");
            if (logViewer != null)
            {
                logViewer.Clear();
            }
        }

        private void OnReconnectClick(object? sender, RoutedEventArgs e)
        {
            _ = ConnectWebSocketAsync();
        }

        /// <summary>
        /// 打开独立窗口
        /// </summary>
        private void OnOpenWindowClick(object? sender, RoutedEventArgs e)
        {
            OpenLogWindowInternal();
        }

        /// <summary>
        /// 内部方法：打开日志独立窗口
        /// </summary>
        private void OpenLogWindowInternal()
        {
            if (_logWindow != null)
            {
                // 如果窗口已存在，激活它
                _logWindow.Activate();
                return;
            }

            // 获取当前日志内容
            var logViewer = this.FindControl<Controls.LogViewer>("LogViewer");
            var existingLogs = logViewer?.GetAllLogs() ?? "";

            // 创建独立窗口
            _logWindow = new Windows.LogWindow();

            // 复制日志内容到独立窗口
            if (!string.IsNullOrEmpty(existingLogs))
            {
                _logWindow.SetInitialLogs(existingLogs);
            }

            // 断开原页面的WebSocket连接（由独立窗口接管）
            DisconnectWebSocket();

            // 隐藏原页面的日志容器和工具栏
            var logContainer = this.FindControl<Border>("LogContainer");
            if (logContainer != null)
            {
                logContainer.IsVisible = false;
            }

            var toolbarPanel = this.FindControl<StackPanel>("ToolbarPanel");
            if (toolbarPanel != null)
            {
                toolbarPanel.IsVisible = false;
            }

            _isInSeparateWindow = true;

            // 注册到桌面模式管理器
            DesktopModeManager.Instance.RegisterLogWindow(_logWindow);

            // 监听窗口关闭事件
            _logWindow.Closed += (s, args) =>
            {
                // 清空静态变量
                _logWindow = null;
                _isInSeparateWindow = false;

                // 断开WebSocket连接
                DisconnectWebSocket();

                // 触发静态事件，通知所有日志页面实例
                LogWindowClosed?.Invoke(this, EventArgs.Empty);
            };

            // 获取父窗口并显示独立窗口
            if (VisualRoot is Window parentWindow)
            {
                _logWindow.Show(parentWindow);
            }
            else
            {
                _logWindow.Show();
            }
        }

        public bool IsConnected => _isConnected;

        /// <summary>
        /// 静态方法：打开日志独立窗口（供全局快捷键调用）
        /// </summary>
        public static void OpenLogWindowStatic(Window? parentWindow = null)
        {
            // 如果独立窗口已存在，激活它
            if (_logWindow != null)
            {
                _logWindow.Activate();
                return;
            }

            // 创建新的独立窗口
            _logWindow = new Windows.LogWindow();

            // 注册到桌面模式管理器
            DesktopModeManager.Instance.RegisterLogWindow(_logWindow);

            // 监听窗口关闭事件
            _logWindow.Closed += (s, args) =>
            {
                _logWindow = null;

                // 触发静态事件，通知所有日志页面实例
                LogWindowClosed?.Invoke(null, EventArgs.Empty);
            };

            // 显示窗口（如果有父窗口则作为子窗口显示）
            if (parentWindow != null)
            {
                _logWindow.Show(parentWindow);
            }
            else
            {
                _logWindow.Show();
            }
        }

        /// <summary>
        /// 去除 ANSI 转义序列（如颜色代码）
        /// </summary>
        private string RemoveAnsiCodes(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            // 匹配 ANSI 转义序列: \u001b[XXm 或 \u001b[XXXm
            var ansiPattern = "\u001b\\[[0-9;]*m";
            return System.Text.RegularExpressions.Regex.Replace(input, ansiPattern, "");
        }
    }

    public class WebSocketMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string? Type { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public string? Data { get; set; }
    }
}
