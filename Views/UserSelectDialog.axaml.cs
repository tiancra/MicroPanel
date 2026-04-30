using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using MicroPanelAvalonia.Models;
using MicroPanelAvalonia.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MicroPanelAvalonia.Views
{
    public partial class UserSelectDialog : UserControl
    {
        private ServerInfo? _serverInfo;
        private bool _isProcessing;
        private readonly HttpClient _httpClient = new();

        public event EventHandler? Cancelled;
        public event EventHandler<(ServerInfo server, ServerUser user, string token)>? UserSelected;
        public event EventHandler<(ServerInfo server, ServerUser user)>? ShowUserConfig;

        public UserSelectDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void SetServer(ServerInfo server)
        {
            _serverInfo = server;

            var serverTextBlock = this.FindControl<TextBlock>("ServerTextBlock");
            if (serverTextBlock != null)
            {
                serverTextBlock.Text = server.ServerAddress;
            }

            var usersListBox = this.FindControl<ListBox>("UsersListBox");
            if (usersListBox != null)
            {
                usersListBox.ItemsSource = server.Users;
                if (server.Users.Any())
                {
                    usersListBox.SelectedIndex = 0;
                }
            }
        }

        private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!_isProcessing)
            {
                Cancelled?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void OnLoginClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_isProcessing || _serverInfo == null) return;

            var usersListBox = this.FindControl<ListBox>("UsersListBox");
            if (usersListBox?.SelectedItem is not ServerUser selectedUser)
            {
                ShowError("请选择一个用户");
                return;
            }

            SetProcessing(true);

            try
            {
                // 第一步：直接请求登录接口获取Token（不使用缓存）
                var loginResult = await LoginAsync(selectedUser);

                if (!loginResult.success)
                {
                    ShowError(loginResult.token);
                    return;
                }

                var token = loginResult.token;

                // 第二步：使用Token请求用户信息接口，检查是否403
                var userInfoResult = await GetUserInfoAsync(token);

                if (userInfoResult?.Code == 403)
                {
                    // Token过期，使用账号密码重新登录
                    var reLoginResult = await ReLoginAsync(selectedUser);

                    if (!reLoginResult.success)
                    {
                        // 第二次还是403，弹窗提示并打开用户配置
                        ShowAccountExpiredDialog();
                        ShowUserConfig?.Invoke(this, (_serverInfo, selectedUser));
                        return;
                    }

                    token = reLoginResult.token;

                    // 再次检查用户信息
                    userInfoResult = await GetUserInfoAsync(token);
                    if (userInfoResult?.Code == 403)
                    {
                        // 第二次还是403，弹窗提示并打开用户配置
                        ShowAccountExpiredDialog();
                        ShowUserConfig?.Invoke(this, (_serverInfo, selectedUser));
                        return;
                    }
                }

                // 登录成功，触发事件
                UserSelected?.Invoke(this, (_serverInfo, selectedUser, token));
            }
            catch (Exception ex)
            {
                ShowError($"登录失败: {ex.Message}");
            }
            finally
            {
                SetProcessing(false);
            }
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        private async Task<ApiResponse<UserInfoResponse>?> GetUserInfoAsync(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", token);
                var response = await _httpClient.GetAsync($"{_serverInfo?.ServerAddress}/api/user/info");
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<UserInfoResponse>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserSelectDialog] GetUserInfo error: {ex}");
                return null;
            }
        }

        /// <summary>
        /// 登录（直接请求接口，不使用缓存）
        /// </summary>
        private async Task<(bool success, string token)> LoginAsync(ServerUser user)
        {
            try
            {
                var loginData = new
                {
                    username = user.Username,
                    password = user.Password
                };
                var content = new StringContent(
                    JsonSerializer.Serialize(loginData),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_serverInfo?.ServerAddress}/api/login",
                    content);

                var json = await response.Content.ReadAsStringAsync();
                
                // 先解析为 JsonDocument 来处理 data 可能是字符串的情况
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                var code = root.GetProperty("code").GetInt32();
                if (code != 200)
                {
                    var msg = root.TryGetProperty("message", out var msgElement) 
                        ? msgElement.GetString() 
                        : "登录失败";
                    return (false, msg ?? "登录失败");
                }

                // data 可能是字符串直接作为 token
                var dataElement = root.GetProperty("data");
                string? token = null;
                
                if (dataElement.ValueKind == JsonValueKind.String)
                {
                    token = dataElement.GetString();
                }
                else if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    // 尝试获取 token 属性
                    if (dataElement.TryGetProperty("token", out var tokenProp))
                    {
                        token = tokenProp.GetString();
                    }
                }

                if (!string.IsNullOrEmpty(token))
                {
                    return (true, token);
                }

                return (false, "登录失败：无法获取Token");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserSelectDialog] Login error: {ex}");
                return (false, $"登录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 重新登录
        /// </summary>
        private async Task<(bool success, string token)> ReLoginAsync(ServerUser user)
        {
            try
            {
                var loginData = new
                {
                    username = user.Username,
                    password = user.Password
                };
                var content = new StringContent(
                    JsonSerializer.Serialize(loginData),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_serverInfo?.ServerAddress}/api/login",
                    content);

                var json = await response.Content.ReadAsStringAsync();
                
                // 先解析为 JsonDocument 来处理 data 可能是字符串的情况
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                var code = root.GetProperty("code").GetInt32();
                if (code != 200)
                {
                    return (false, "重新登录失败");
                }

                // data 可能是字符串直接作为 token
                var dataElement = root.GetProperty("data");
                string? token = null;
                
                if (dataElement.ValueKind == JsonValueKind.String)
                {
                    token = dataElement.GetString();
                }
                else if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    if (dataElement.TryGetProperty("token", out var tokenProp))
                    {
                        token = tokenProp.GetString();
                    }
                }

                if (!string.IsNullOrEmpty(token))
                {
                    return (true, token);
                }

                return (false, "重新登录失败：无法获取Token");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserSelectDialog] ReLogin error: {ex}");
                return (false, $"重新登录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示账号过期弹窗
        /// </summary>
        private void ShowAccountExpiredDialog()
        {
            var dialog = new Window
            {
                Title = "提示",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new TextBlock
                {
                    Text = "账号已过期",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
            };
            dialog.ShowDialog((Window)this.GetVisualRoot()!);
        }

        private void ShowError(string message)
        {
            var errorPanel = this.FindControl<StackPanel>("ErrorPanel");
            var errorTextBlock = this.FindControl<TextBlock>("ErrorTextBlock");

            if (errorPanel != null && errorTextBlock != null)
            {
                errorTextBlock.Text = message;
                errorPanel.IsVisible = true;
            }
        }

        private void SetProcessing(bool processing)
        {
            _isProcessing = processing;

            var progressPanel = this.FindControl<StackPanel>("ProgressPanel");
            var loginButton = this.FindControl<Button>("LoginButton");
            var cancelButton = this.FindControl<Button>("CancelButton");
            var errorPanel = this.FindControl<StackPanel>("ErrorPanel");

            if (progressPanel != null)
                progressPanel.IsVisible = processing;

            if (loginButton != null)
                loginButton.IsEnabled = !processing;

            if (cancelButton != null)
                cancelButton.IsEnabled = !processing;

            if (errorPanel != null && processing)
                errorPanel.IsVisible = false;
        }

        public void Reset()
        {
            var usersListBox = this.FindControl<ListBox>("UsersListBox");
            var errorPanel = this.FindControl<StackPanel>("ErrorPanel");

            if (usersListBox != null)
                usersListBox.SelectedIndex = -1;

            if (errorPanel != null)
                errorPanel.IsVisible = false;

            SetProcessing(false);
        }
    }
}
