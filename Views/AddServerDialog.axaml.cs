using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using MicroPanelAvalonia.Services;
using System;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;

namespace MicroPanelAvalonia.Views
{
    public partial class AddServerDialog : UserControl
    {
        private readonly ApiService _apiService;
        private bool _isProcessing;
        private CancellationTokenSource? _soundCancellationTokenSource;
        private readonly object _soundLock = new();

        public event EventHandler? Cancelled;
        public event EventHandler<(string serverAddress, string username, string password)>? Confirmed;

        public AddServerDialog()
        {
            InitializeComponent();
            _apiService = new ApiService();

            var serverAddressTextBox = this.FindControl<TextBox>("ServerAddressTextBox");
            var usernameTextBox = this.FindControl<TextBox>("UsernameTextBox");
            var passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");

            if (serverAddressTextBox != null)
            {
                serverAddressTextBox.KeyDown += (s, e) =>
                {
                    if (e.Key == Avalonia.Input.Key.Enter)
                    {
                        usernameTextBox?.Focus();
                    }
                };
            }

            if (usernameTextBox != null)
            {
                usernameTextBox.KeyDown += (s, e) =>
                {
                    if (e.Key == Avalonia.Input.Key.Enter)
                    {
                        passwordTextBox?.Focus();
                    }
                };
            }

            if (passwordTextBox != null)
            {
                passwordTextBox.KeyDown += async (s, e) =>
                {
                    if (e.Key == Avalonia.Input.Key.Enter)
                    {
                        await OnConfirmClickAsync(s, e);
                    }
                };
            }
        }

        private async void OnConfirmClickHandler(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await OnConfirmClickAsync(sender, e);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!_isProcessing)
            {
                Cancelled?.Invoke(this, EventArgs.Empty);
            }
        }

        private async Task OnConfirmClickAsync(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_isProcessing) return;

            var serverAddress = this.FindControl<TextBox>("ServerAddressTextBox")?.Text?.Trim() ?? string.Empty;
            var username = this.FindControl<TextBox>("UsernameTextBox")?.Text?.Trim() ?? string.Empty;
            var password = this.FindControl<TextBox>("PasswordTextBox")?.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                ShowError("请输入服务器地址");
                return;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("请输入账号");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("请输入密码");
                return;
            }

            SetProcessing(true);

            try
            {
                var normalizedAddress = serverAddress;
                if (!normalizedAddress.StartsWith("http://") && !normalizedAddress.StartsWith("https://"))
                {
                    normalizedAddress = "http://" + normalizedAddress;
                }

                _apiService.SetBaseUrl(normalizedAddress);
                var response = await _apiService.LoginAsync(username, password);

                if (response?.IsSuccess == true)
                {
                    Confirmed?.Invoke(this, (serverAddress, username, password));
                }
                else
                {
                    ShowError(response?.Data ?? "登录失败，请检查账号密码");
                }
            }
            catch (Exception ex)
            {
                ShowError($"连接失败: {ex.Message}");
            }
            finally
            {
                SetProcessing(false);
            }
        }

        public void ShowError(string message)
        {
            var errorPanel = this.FindControl<StackPanel>("ErrorPanel");
            var errorTextBlock = this.FindControl<TextBlock>("ErrorTextBlock");

            if (errorPanel != null && errorTextBlock != null)
            {
                errorTextBlock.Text = message;
                errorPanel.IsVisible = true;
                PlayErrorSound();
            }
        }

        /// <summary>
        /// 播放错误音效（异步，不阻塞主线程，支持中断）
        /// </summary>
        private void PlayErrorSound()
        {
            lock (_soundLock)
            {
                // 取消上一个正在播放的音频
                _soundCancellationTokenSource?.Cancel();
                _soundCancellationTokenSource?.Dispose();
                _soundCancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _soundCancellationTokenSource.Token;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var uri = new Uri("avares://MicroPanelAvalonia/Assets/error.wav");
                        using var stream = AssetLoader.Open(uri);
                        using var memoryStream = new MemoryStream();
                        stream.CopyTo(memoryStream);
                        memoryStream.Position = 0;

                        // 检查是否已取消
                        cancellationToken.ThrowIfCancellationRequested();

                        using var player = new SoundPlayer(memoryStream);
                        player.Play(); // 异步播放

                        // 等待播放完成或取消
                        await Task.Delay(500, cancellationToken); // 假设音频最长500ms
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常取消，不需要处理
                    }
                    catch
                    {
                        // 音效播放失败时静默处理
                    }
                }, cancellationToken);
            }
        }

        private void SetProcessing(bool processing)
        {
            _isProcessing = processing;

            var progressPanel = this.FindControl<StackPanel>("ProgressPanel");
            var confirmButton = this.FindControl<Button>("ConfirmButton");
            var cancelButton = this.FindControl<Button>("CancelButton");
            var errorPanel = this.FindControl<StackPanel>("ErrorPanel");

            if (progressPanel != null)
                progressPanel.IsVisible = processing;

            if (confirmButton != null)
                confirmButton.IsEnabled = !processing;

            if (cancelButton != null)
                cancelButton.IsEnabled = !processing;

            if (errorPanel != null && processing)
                errorPanel.IsVisible = false;
        }

        public void Reset()
        {
            var serverAddressTextBox = this.FindControl<TextBox>("ServerAddressTextBox");
            var usernameTextBox = this.FindControl<TextBox>("UsernameTextBox");
            var passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
            var errorPanel = this.FindControl<StackPanel>("ErrorPanel");

            if (serverAddressTextBox != null)
                serverAddressTextBox.Text = string.Empty;

            if (usernameTextBox != null)
                usernameTextBox.Text = string.Empty;

            if (passwordTextBox != null)
                passwordTextBox.Text = string.Empty;

            if (errorPanel != null)
                errorPanel.IsVisible = false;

            SetProcessing(false);
        }
    }
}
