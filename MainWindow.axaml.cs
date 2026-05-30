using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using MicroPanel.Models;
using MicroPanel.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using MicroPanel.Views.Pages;
using MicroPanel.Helpers.UI;
using MicroPanel.Views;
using MicroPanel.Controls;

namespace MicroPanel;

public partial class MainWindow : MyWindow
{
    private readonly ApiService _apiService;
    private readonly ServerManager _serverManager;
    private readonly Timer _refreshTimer;
    private ServerInfo? _currentContextServer;

    public MainWindow()
    {
        InitializeComponent();

        _apiService = new ApiService();
        _serverManager = new ServerManager(_apiService);

        _refreshTimer = new Timer(5000);
        _refreshTimer.Elapsed += async (s, e) => await _serverManager.RefreshAllServersAsync();
        _refreshTimer.Start();

        LoadServers();

        DesktopModeManager.Instance.RegisterMainWindow(this);

        if (DebugModeService.IsDebugMode)
        {
            ShowDebugModeWatermark();
        }

        Loaded += async (s, e) => 
        {
            await _serverManager.RefreshAllServersAsync();
            
            if (DebugModeService.IsDebugMode)
            {
                await DebugModeService.ShowDebugModeWarningDialog(this);
                
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    OpenDebugMenu();
                }, DispatcherPriority.Background);
            }
        };
    }

    private void OpenDebugMenu()
    {
        try
        {
            DebugModeService.LogDebug("正在创建调试菜单窗口...");
            var debugMenu = new DebugMenuWindow();
            debugMenu.Show();
            DebugModeService.LogDebug("调试菜单窗口已显示（无法关闭）");
        }
        catch (Exception ex)
        {
            DebugModeService.LogDebug($"打开调试菜单失败: {ex}");
        }
    }

    private void ShowDebugModeWatermark()
    {
        var watermark = this.FindControl<Border>("DebugModeWatermark");
        if (watermark != null)
        {
            watermark.IsVisible = true;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        var navigationView = this.FindControl<NavigationView>("NavigationView");
        if (navigationView != null)
        {
            navigationView.SelectionChanged += NavigationView_SelectionChanged;
        }
    }
    
    private void NavigationView_SelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            string pageTitle = tag;
            if (item.Content is TextBlock textBlock)
            {
                pageTitle = textBlock.Text ?? tag;
            }
            else if (item.Content != null)
            {
                pageTitle = item.Content.ToString() ?? tag;
            }
            
            switch (tag)
            {
                case "Home":
                    ShowServerListView();
                    this.Title = $"Micro Panel - {pageTitle}";
                    break;
                case "Settings":
                    ShowSettingsView();
                    this.Title = $"Micro Panel - {pageTitle}";
                    break;
                case "About":
                    ShowAboutView();
                    this.Title = $"Micro Panel - {pageTitle}";
                    break;
            }
        }
    }
    
    private void ShowServerListView()
    {
        var serverListView = this.FindControl<Grid>("ServerListView");
        var settingsView = this.FindControl<SettingsPage>("SettingsView");
        var aboutView = this.FindControl<AboutPage>("AboutView");
        
        if (serverListView != null) serverListView.IsVisible = true;
        if (settingsView != null) settingsView.IsVisible = false;
        if (aboutView != null) aboutView.IsVisible = false;
    }
    
    private void ShowSettingsView()
    {
        var serverListView = this.FindControl<Grid>("ServerListView");
        var settingsView = this.FindControl<SettingsPage>("SettingsView");
        var aboutView = this.FindControl<AboutPage>("AboutView");
        
        if (serverListView != null) serverListView.IsVisible = false;
        if (settingsView != null) settingsView.IsVisible = true;
        if (aboutView != null) aboutView.IsVisible = false;
    }
    
    private void ShowAboutView()
    {
        var serverListView = this.FindControl<Grid>("ServerListView");
        var settingsView = this.FindControl<SettingsPage>("SettingsView");
        var aboutView = this.FindControl<AboutPage>("AboutView");
        
        if (serverListView != null) serverListView.IsVisible = false;
        if (settingsView != null) settingsView.IsVisible = false;
        if (aboutView != null) aboutView.IsVisible = true;
    }

    private void LoadServers()
    {
        var itemsControl = this.FindControl<ItemsControl>("ServersItemsControl");
        if (itemsControl == null) return;

        itemsControl.ItemsSource = null;

        var serverCards = new List<ServerCard>();
        foreach (var server in _serverManager.Servers)
        {
            var card = new ServerCard
            {
                DataContext = server,
                Margin = new Thickness(0, 0, 16, 16)
            };
            card.CardClicked += OnServerCardClicked;
            card.UserManagementRequested += OnUserManagementRequested;
            card.DeleteRequested += OnDeleteRequested;
            card.EditRequested += OnEditRequested;
            serverCards.Add(card);
        }

        itemsControl.ItemsSource = serverCards;
    }

    private async void OnEditRequested(object? sender, ServerInfo server)
    {
        var result = await MicroPanelDialogs.ShowAddServerDialog(server, this);
        if (result.success)
        {
            var updateResult = await _serverManager.UpdateServerAsync(server, result.serverAddress!, result.username!, result.password!, result.serverName);
            if (updateResult.success)
            {
                LoadServers();
                ToastService.Instance.ShowSuccess("服务器更新成功");
            }
            else
            {
                await CommonTaskDialogs.ShowDialog("更新失败", updateResult.message, this);
            }
        }
    }

    private async void OnUserManagementRequested(object? sender, ServerInfo server)
    {
        while (true)
        {
            var result = await MicroPanelDialogs.ShowUserManagementDialog(server, this);
            switch (result.action)
            {
                case MicroPanelDialogs.UserManagementAction.EditUser:
                    if (result.targetUser != null)
                    {
                        var editResult = await MicroPanelDialogs.ShowEditUserDialog(result.targetUser, this);
                        if (editResult.success && !string.IsNullOrWhiteSpace(editResult.password))
                        {
                            _serverManager.UpdateUser(server, result.targetUser, editResult.password);
                            LoadServers();
                        }
                    }
                    break;

                case MicroPanelDialogs.UserManagementAction.DeleteUser:
                    if (result.targetUser != null)
                    {
                        var confirm = await MicroPanelDialogs.ShowConfirmDialog("删除用户", $"确定要删除用户 \"{result.targetUser.Username}\" 吗？", this);
                        if (confirm)
                        {
                            _serverManager.RemoveUser(server, result.targetUser);
                            LoadServers();
                        }
                    }
                    break;

                case MicroPanelDialogs.UserManagementAction.AddUser:
                    var addResult = await MicroPanelDialogs.ShowEditUserDialog(null, this);
                    if (addResult.success && !string.IsNullOrWhiteSpace(addResult.username) && !string.IsNullOrWhiteSpace(addResult.password))
                    {
                        var addResult2 = await _serverManager.AddUserToServerAsync(server, addResult.username, addResult.password);
                        if (!addResult2.success)
                        {
                            await CommonTaskDialogs.ShowDialog("添加失败", addResult2.message, this);
                        }
                        else
                        {
                            LoadServers();
                        }
                    }
                    break;

                default:
                    return;
            }
        }
    }

    private async void OnDeleteRequested(object? sender, ServerInfo server)
    {
        var confirmed = await MicroPanelDialogs.ShowConfirmDialog("删除服务器", $"确定要删除服务器 \"{server.ServerName}\" 吗？", this);
        if (confirmed)
        {
            _serverManager.RemoveServer(server);
            LoadServers();
        }
    }

    private async void OnAddServerClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var result = await MicroPanelDialogs.ShowAddServerDialog(null, this);
        if (result.success)
        {
            var addResult = await _serverManager.AddServerAsync(result.serverAddress!, result.username!, result.password!, result.serverName);
            if (addResult.success)
            {
                LoadServers();
            }
            else
            {
                await CommonTaskDialogs.ShowDialog("添加失败", addResult.message, this);
            }
        }
    }

    private async void OnServerCardClicked(object? sender, ServerInfo server)
    {
        if (!server.IsOnline)
        {
            await CommonTaskDialogs.ShowDialog("服务器离线", "该服务器当前处于离线状态，无法进入主界面。", this);
            return;
        }
        
        if (server.Users.Count == 1)
        {
            _ = LoginWithUserAsync(server, server.Users[0]);
        }
        else if (server.Users.Count > 1)
        {
            var result = await MicroPanelDialogs.ShowUserSelectDialog(server, this);
            if (result.success && result.user != null)
            {
                _ = LoginWithUserAsync(server, result.user);
            }
        }
    }

    private async Task LoginWithUserAsync(ServerInfo server, ServerUser user)
    {
        var result = await _serverManager.LoginWithUserAsync(server, user);
        if (result.success && result.token != null)
        {
            await OnUserLoggedInAsync(server, user, result.token);
        }
        else
        {
            await CommonTaskDialogs.ShowDialog("登录失败", result.token ?? "未知错误", this);
        }
    }

    private async Task OnUserLoggedInAsync(ServerInfo server, ServerUser user, string token)
    {
        Console.WriteLine($"用户 {user.Username} 已登录服务器 {server.ServerAddress}");
        Console.WriteLine($"Token: {token}");

        // 获取用户信息以获取 routes
        _apiService.SetBaseUrl(server.ServerAddress);
        var userInfoResponse = await _apiService.GetUserInfoAsync(token);
        
        // 如果返回 403 "未找到该用户登录"，尝试刷新 Token
        if (userInfoResponse?.Code == 403 && userInfoResponse?.Message?.Contains("未找到该用户登录") == true)
        {
            Console.WriteLine("Token 已过期，尝试重新登录...");
            
            // 使用保存的密码重新登录
            var loginResponse = await _apiService.LoginAsync(user.Username, user.Password);
            
            if (loginResponse?.IsSuccess == true && !string.IsNullOrEmpty(loginResponse.Data))
            {
                // 更新本地 Token
                var newToken = loginResponse.Data;
                user.Token = newToken;
                user.TokenExpiry = DateTime.Now.AddHours(24);
                _serverManager.SaveServers();
                
                Console.WriteLine($"Token 刷新成功，新 Token: {newToken}");
                
                // 使用新 Token 重新获取用户信息
                userInfoResponse = await _apiService.GetUserInfoAsync(newToken);
                token = newToken;
                
                // 如果还是失败，说明账号真的过期了
                if (userInfoResponse?.Code == 403)
                {
                    Console.WriteLine("Token 刷新后仍然失败，账号已过期");
                    await CommonTaskDialogs.ShowDialog("登录失败", "账号已过期，请重新登录", this);
                    return;
                }
            }
            else
            {
                // 重新登录失败
                Console.WriteLine($"重新登录失败: {loginResponse?.Message}");
                await CommonTaskDialogs.ShowDialog("登录失败", "账号已过期，请重新登录", this);
                return;
            }
        }
        
        var userRoutes = userInfoResponse?.Data?.Routes;

        Console.WriteLine($"用户路由权限: {string.Join(", ", userRoutes ?? new List<string>())}");

        // 启动会话并存储 routes
        SessionService.Instance.StartSession(server, user, token, userRoutes);

        var mainAppWindow = new Views.MainAppWindow();
        mainAppWindow.Show();

        Hide();

        mainAppWindow.Closed += (s, e) =>
        {
            Show();
            _ = _serverManager.RefreshAllServersAsync();
        };
    }

    private async void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await _serverManager.RefreshAllServersAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        base.OnClosed(e);
    }

    public void ShowServerList()
    {
        var serverListView = this.FindControl<Grid>("ServerListView");
        var settingsView = this.FindControl<SettingsPage>("SettingsView");
        var aboutView = this.FindControl<AboutPage>("AboutView");

        if (serverListView != null)
        {
            serverListView.IsVisible = true;
            settingsView!.IsVisible = false;
            aboutView!.IsVisible = false;
        }
    }
}