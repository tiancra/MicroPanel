using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using FluentAvalonia.Styling;
using MicroPanelAvalonia.Models;
using MicroPanelAvalonia.Services;
using MicroPanelAvalonia.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using MicroPanelAvalonia.Views.Pages;

namespace MicroPanelAvalonia
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly ServerManager _serverManager;
        private readonly Timer _refreshTimer;
        private ServerInfo? _currentContextServer;
        private ServerUser? _currentEditingUser;

        public MainWindow()
        {
            InitializeComponent();

            _apiService = new ApiService();
            _serverManager = new ServerManager(_apiService);

            _refreshTimer = new Timer(5000);
            _refreshTimer.Elapsed += async (s, e) => await _serverManager.RefreshAllServersAsync();
            _refreshTimer.Start();

            SetupConverters();
            SetupDialogs();
            LoadServers();

            // 注册到桌面模式管理器
            DesktopModeManager.Instance.RegisterMainWindow(this);

            // 如果是调试模式，显示水印
            if (DebugModeService.IsDebugMode)
            {
                ShowDebugModeWatermark();
            }

            // 窗口加载完成后立即刷新所有服务器状态
            Loaded += async (s, e) => 
            {
                await _serverManager.RefreshAllServersAsync();
                
                // 如果是调试模式，显示警告弹窗并打开调试菜单
                if (DebugModeService.IsDebugMode)
                {
                    await DebugModeService.ShowDebugModeWarningDialog(this);
                    
                    // 打开调试菜单（无法关闭）
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        OpenDebugMenu();
                    }, DispatcherPriority.Background);
                }
            };

        }



        /// <summary>
        /// 打开调试菜单（无法关闭）
        /// </summary>
        private void OpenDebugMenu()
        {
            try
            {
                DebugModeService.LogDebug("正在创建调试菜单窗口...");
                var debugMenu = new DebugMenuWindow();
                
                // 注意：关闭事件阻止已在 DebugMenuWindow 构造函数中处理
                
                debugMenu.Show();
                DebugModeService.LogDebug("调试菜单窗口已显示（无法关闭）");
            }
            catch (Exception ex)
            {
                DebugModeService.LogDebug($"打开调试菜单失败: {ex}");
            }
        }

        /// <summary>
        /// 显示调试模式水印
        /// </summary>
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
        }

        private void SetupConverters()
        {
            // 转换器已在 App.axaml 中定义
        }

        private void SetupDialogs()
        {
            // 添加服务器弹窗
            var addServerDialog = this.FindControl<AddServerDialog>("AddServerDialog");
            if (addServerDialog != null)
            {
                addServerDialog.Cancelled += (s, e) => HideAddServerDialog();
                addServerDialog.Confirmed += async (s, e) =>
                {
                    var result = await _serverManager.AddServerAsync(e.serverAddress, e.username, e.password);
                    if (result.success)
                    {
                        HideAddServerDialog();
                        LoadServers();
                    }
                    else
                    {
                        addServerDialog.ShowError(result.message);
                    }
                };
            }

            // 用户选择弹窗
            var userSelectDialog = this.FindControl<UserSelectDialog>("UserSelectDialog");
            if (userSelectDialog != null)
            {
                userSelectDialog.Cancelled += (s, e) => HideUserSelectDialog();
                userSelectDialog.UserSelected += (s, e) =>
                {
                    HideUserSelectDialog();
                    OnUserLoggedIn(e.server, e.user, e.token);
                };
                userSelectDialog.ShowUserConfig += (s, e) =>
                {
                    HideUserSelectDialog();
                    _currentContextServer = e.server;
                    ShowUserManagementDialog(e.server);
                    // 选中当前用户并打开编辑
                    var userManagementDialog = this.FindControl<UserManagementDialog>("UserManagementDialog");
                    userManagementDialog?.SelectUser(e.user);
                };
            }

            // 用户管理弹窗
            var userManagementDialog = this.FindControl<UserManagementDialog>("UserManagementDialog");
            if (userManagementDialog != null)
            {
                userManagementDialog.CloseRequested += (s, e) => HideUserManagementDialog();
                userManagementDialog.EditUserRequested += (s, user) =>
                {
                    _currentEditingUser = user;
                    ShowEditUserDialog(user);
                };
                userManagementDialog.DeleteUserRequested += (s, user) =>
                {
                    ShowConfirmDialog(
                        "删除用户",
                        $"确定要删除用户 \"{user.Username}\" 吗？",
                        () =>
                        {
                            if (_currentContextServer != null)
                            {
                                _serverManager.RemoveUser(_currentContextServer, user);
                                userManagementDialog.RefreshUsersList();
                            }
                        });
                };
                userManagementDialog.AddUserRequested += (s, e) =>
                {
                    ShowAddUserDialog();
                };
            }

            // 编辑/添加用户弹窗
            var editUserDialog = this.FindControl<EditUserDialog>("EditUserDialog");
            if (editUserDialog != null)
            {
                editUserDialog.Cancelled += (s, e) => HideEditUserDialog();
                editUserDialog.Confirmed += async (s, e) =>
                {
                    if (_currentContextServer == null) return;

                    if (_currentEditingUser != null)
                    {
                        // 编辑模式 - 更新密码
                        _serverManager.UpdateUser(_currentContextServer, _currentEditingUser, e.password);
                        HideEditUserDialog();
                        var userManagementDialog2 = this.FindControl<UserManagementDialog>("UserManagementDialog");
                        userManagementDialog2?.RefreshUsersList();
                    }
                    else
                    {
                        // 添加模式
                        var result = await _serverManager.AddUserToServerAsync(_currentContextServer, e.username, e.password);
                        if (result.success)
                        {
                            HideEditUserDialog();
                            var userManagementDialog2 = this.FindControl<UserManagementDialog>("UserManagementDialog");
                            userManagementDialog2?.RefreshUsersList();
                        }
                        else
                        {
                            editUserDialog.ShowError(result.message);
                        }
                    }
                };
            }

            // 确认弹窗
            var confirmDialog = this.FindControl<ConfirmDialog>("ConfirmDialogControl");
            if (confirmDialog != null)
            {
                confirmDialog.Cancelled += (s, e) => HideConfirmDialog();
                confirmDialog.Confirmed += (s, e) =>
                {
                    HideConfirmDialog();
                    _confirmAction?.Invoke();
                };
            }
        }

        private Action? _confirmAction;

        private void ShowConfirmDialog(string title, string message, Action onConfirm)
        {
            _confirmAction = onConfirm;
            var confirmDialog = this.FindControl<ConfirmDialog>("ConfirmDialogControl");
            confirmDialog?.SetContent(title, message);
            ShowOverlay("ConfirmOverlay", "ConfirmDialogContainer");
        }

        private void HideConfirmDialog()
        {
            HideOverlay("ConfirmOverlay", "ConfirmDialogContainer");
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
                serverCards.Add(card);
            }

            itemsControl.ItemsSource = serverCards;
        }

        private void OnUserManagementRequested(object? sender, ServerInfo server)
        {
            _currentContextServer = server;
            ShowUserManagementDialog(server);
        }

        private void OnDeleteRequested(object? sender, ServerInfo server)
        {
            ShowConfirmDialog(
                "删除服务器",
                $"确定要删除服务器 \"{server.ServerAddress}\" 吗？",
                () =>
                {
                    _serverManager.RemoveServer(server);
                    LoadServers();
                });
        }

        private void ShowUserManagementDialog(ServerInfo server)
        {
            var dialog = this.FindControl<UserManagementDialog>("UserManagementDialog");
            dialog?.SetServer(server);
            ShowOverlay("UserManagementOverlay", "UserManagementDialogContainer");
        }

        private void HideUserManagementDialog()
        {
            HideOverlay("UserManagementOverlay", "UserManagementDialogContainer");
            _currentContextServer = null;
        }

        private void ShowEditUserDialog(ServerUser user)
        {
            var dialog = this.FindControl<EditUserDialog>("EditUserDialog");
            dialog?.SetEditMode(user);
            dialog?.Reset();
            ShowOverlay("EditUserOverlay", "EditUserDialogContainer");
        }

        private void ShowAddUserDialog()
        {
            _currentEditingUser = null;
            var dialog = this.FindControl<EditUserDialog>("EditUserDialog");
            dialog?.SetAddMode();
            dialog?.Reset();
            ShowOverlay("EditUserOverlay", "EditUserDialogContainer");
        }

        private void HideEditUserDialog()
        {
            HideOverlay("EditUserOverlay", "EditUserDialogContainer");
        }

        private void ShowOverlay(string overlayName, string containerName)
        {
            var overlay = this.FindControl<Border>(overlayName);
            var container = this.FindControl<Border>(containerName);

            if (overlay != null && container != null)
            {
                overlay.IsVisible = true;
                Dispatcher.UIThread.Post(() =>
                {
                    overlay.Opacity = 1;
                    container.Opacity = 1;
                    container.RenderTransform = new ScaleTransform(1, 1);
                }, DispatcherPriority.Render);
            }
        }

        private void HideOverlay(string overlayName, string containerName)
        {
            var overlay = this.FindControl<Border>(overlayName);
            var container = this.FindControl<Border>(containerName);

            if (overlay != null && container != null)
            {
                overlay.Opacity = 0;
                container.Opacity = 0;
                container.RenderTransform = new ScaleTransform(0.9, 0.9);

                Dispatcher.UIThread.Post(async () =>
                {
                    await Task.Delay(250);
                    overlay.IsVisible = false;
                }, DispatcherPriority.Background);
            }
        }

        private void OnAddServerClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ShowOverlay("AddServerOverlay", "AddServerDialogContainer");
            var dialog = this.FindControl<AddServerDialog>("AddServerDialog");
            dialog?.Reset();
        }

        private void HideAddServerDialog()
        {
            HideOverlay("AddServerOverlay", "AddServerDialogContainer");
        }

        private void OnServerCardClicked(object? sender, ServerInfo server)
        {
            // 检查服务器是否在线
            if (!server.IsOnline)
            {
                ShowError("服务器离线", "该服务器当前处于离线状态，无法进入主界面。");
                return;
            }
            
            if (server.Users.Count == 1)
            {
                _ = LoginWithUserAsync(server, server.Users[0]);
            }
            else if (server.Users.Count > 1)
            {
                ShowUserSelectDialog(server);
            }
        }

        private void ShowUserSelectDialog(ServerInfo server)
        {
            var dialog = this.FindControl<UserSelectDialog>("UserSelectDialog");
            dialog?.SetServer(server);
            dialog?.Reset();
            ShowOverlay("UserSelectOverlay", "UserSelectDialogContainer");
        }

        private void HideUserSelectDialog()
        {
            HideOverlay("UserSelectOverlay", "UserSelectDialogContainer");
        }

        private async Task LoginWithUserAsync(ServerInfo server, ServerUser user)
        {
            var result = await _serverManager.LoginWithUserAsync(server, user);
            if (result.success && result.token != null)
            {
                OnUserLoggedIn(server, user, result.token);
            }
            else
            {
                ShowError("登录失败", result.token ?? "未知错误");
            }
        }

        private void OnUserLoggedIn(ServerInfo server, ServerUser user, string token)
        {
            Console.WriteLine($"用户 {user.Username} 已登录服务器 {server.ServerAddress}");
            Console.WriteLine($"Token: {token}");

            // 创建会话
            SessionService.Instance.StartSession(server, user, token);

            // 打开主应用窗口
            var mainAppWindow = new Views.MainAppWindow();
            mainAppWindow.Show();

            // 隐藏当前窗口（服务器列表窗口）
            Hide();

            // 当主应用窗口关闭时，重新显示当前窗口
            mainAppWindow.Closed += (s, e) =>
            {
                Show();
                // 刷新服务器状态
                _ = _serverManager.RefreshAllServersAsync();
            };
        }

        private async void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await _serverManager.RefreshAllServersAsync();
        }

        private void ShowError(string title, string message)
        {
            Console.WriteLine($"[{title}] {message}");
        }

        protected override void OnClosed(EventArgs e)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            base.OnClosed(e);
        }

        /// <summary>
        /// 主页按钮点击
        /// </summary>
        private void OnHomeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ShowServerList();
        }

        /// <summary>
        /// 设置按钮点击
        /// </summary>
        private void OnSettingsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // 切换显示设置页面
            var serverListView = this.FindControl<Grid>("ServerListView");
            var settingsView = this.FindControl<Views.Pages.SettingsPage>("SettingsView");
            var aboutView = this.FindControl<Views.Pages.AboutPage>("AboutView");

            if (serverListView != null && settingsView != null)
            {
                serverListView.IsVisible = false;
                aboutView!.IsVisible = false;
                settingsView.IsVisible = true;
            }
        }

        /// <summary>
        /// 关于按钮点击
        /// </summary>
        private void OnAboutClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // 切换显示关于页面
            var serverListView = this.FindControl<Grid>("ServerListView");
            var settingsView = this.FindControl<Views.Pages.SettingsPage>("SettingsView");
            var aboutView = this.FindControl<Views.Pages.AboutPage>("AboutView");

            if (serverListView != null && aboutView != null)
            {
                serverListView.IsVisible = false;
                settingsView!.IsVisible = false;
                aboutView.IsVisible = true;
            }
        }

        /// <summary>
        /// 显示服务器列表
        /// </summary>
        public void ShowServerList()
        {
            var serverListView = this.FindControl<Grid>("ServerListView");
            var settingsView = this.FindControl<Views.Pages.SettingsPage>("SettingsView");
            var aboutView = this.FindControl<Views.Pages.AboutPage>("AboutView");

            if (serverListView != null)
            {
                serverListView.IsVisible = true;
                settingsView!.IsVisible = false;
                aboutView!.IsVisible = false;
            }
        }
    }
}
