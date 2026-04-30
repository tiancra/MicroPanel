using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MicroPanelAvalonia.Services;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MicroPanelAvalonia.Views.Windows
{
    public partial class LogWindow : Window
    {
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cts;
        private bool _isConnected = false;
        private StringBuilder _logBuffer = new StringBuilder();
        private const int MaxLogLength = 100000;

        public LogWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closed += OnWindowClosed;
        }

        private string? _initialLogs;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// 设置初始日志内容（在窗口显示前调用）
        /// </summary>
        public void SetInitialLogs(string logs)
        {
            _initialLogs = logs;
        }

        private void OnWindowLoaded(object? sender, EventArgs e)
        {
            // 绑定按钮事件
            var clearButton = this.FindControl<Button>("ClearButton");
            var reconnectButton = this.FindControl<Button>("ReconnectButton");

            if (clearButton != null)
                clearButton.Click += OnClearClick;

            if (reconnectButton != null)
                reconnectButton.Click += OnReconnectClick;

            // 如果有初始日志，加载到LogViewer
            if (!string.IsNullOrEmpty(_initialLogs))
            {
                var logViewer = this.FindControl<Controls.LogViewer>("LogViewer");
                if (logViewer != null)
                {
                    logViewer.SetLogs(_initialLogs);
                }
                _initialLogs = null;
            }

            // 连接 WebSocket
            _ = ConnectWebSocketAsync();
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            DisconnectWebSocket();

            // 通知桌面模式管理器窗口已关闭
            DesktopModeManager.Instance.OnLogWindowClosed();
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

                System.Diagnostics.Debug.WriteLine($"LogWindow: 连接WebSocket: {fullUrl}");

                await _webSocket.ConnectAsync(new Uri(fullUrl), _cts.Token);
                _isConnected = true;
                UpdateConnectionStatus();

                // 开始接收消息
                _ = ReceiveMessagesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogWindow: WebSocket连接失败: {ex.Message}");
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

                    // 解析 JSON 消息，提取日志数据
                    try
                    {
                        var msgObj = JsonSerializer.Deserialize<WebSocketMessage>(message);
                        if (msgObj?.Type == "log" && !string.IsNullOrEmpty(msgObj.Data))
                        {
                            // 过滤掉 [micro-stdout]客户端连接！消息
                            if (msgObj.Data.Contains("[micro-stdout]客户端连接！"))
                            {
                                continue;
                            }

                            // 去除 ANSI 转义序列
                            var cleanData = RemoveAnsiCodes(msgObj.Data);
                            AppendLog(cleanData);
                        }
                    }
                    catch (JsonException)
                    {
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
                System.Diagnostics.Debug.WriteLine($"LogWindow: 接收消息异常: {ex.Message}");
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
                    System.Diagnostics.Debug.WriteLine($"LogWindow: 关闭WebSocket异常: {ex.Message}");
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
            Dispatcher.UIThread.Post(() =>
            {
                var logViewer = this.FindControl<Controls.LogViewer>("LogViewer");
                if (logViewer == null) return;

                logViewer.AppendLog(text);
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

        private void OnClearClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var logViewer = this.FindControl<Controls.LogViewer>("LogViewer");
            if (logViewer != null)
            {
                logViewer.Clear();
            }
        }

        private void OnReconnectClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _ = ConnectWebSocketAsync();
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

        public bool IsConnected => _isConnected;

        public class WebSocketMessage
        {
            [System.Text.Json.Serialization.JsonPropertyName("type")]
            public string? Type { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("data")]
            public string? Data { get; set; }
        }
    }
}
