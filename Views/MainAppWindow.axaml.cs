using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Layout;
using MicroPanel.Models;
using MicroPanel.Services;
using MicroPanel.Views.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using MicroPanel.Controls;

namespace MicroPanel.Views
{
    public partial class MainAppWindow : MyWindow
    {
        private UserControl? _currentPage;
        private Button? _currentMenuButton;
        private readonly HttpClient _httpClient;
        private List<string> _userRoutes = new();

        // Tag 到路由名称的映射
        private readonly Dictionary<string, string> _tagToRouteMap = new()
        {
            ["Status"] = "Status",
            ["Logs"] = "Logs",
            ["Plugins"] = "Plugin",
            ["Files"] = "Fs",
            ["Sandbox"] = "Plugin",
            ["Database"] = "Plugin",
            ["ConfigBot"] = "Bot",
            ["ConfigPlugin"] = "Plugins",
            ["ConfigUser"] = "Permission"
        };

        public MainAppWindow()
        {
            InitializeComponent();
            _httpClient = new HttpClient();

            // 设置窗口图标
            App.SetWindowIcon(this);

            // 注册到桌面模式管理器
            DesktopModeManager.Instance.RegisterMainWindow(this);

            // 初始化 Toast 服务
            ToastService.Instance.Initialize(this);

            // 窗口加载时初始化
            Loaded += async (s, e) => await OnWindowLoadedAsync(s, e);

            // 注册全局键盘事件
            KeyDown += OnWindowKeyDown;
            KeyUp += OnWindowKeyUp;

            // 如果是调试模式，显示水印
            Debug.WriteLine($"MainAppWindow 构造函数 - IsDebugMode: {DebugModeService.IsDebugMode}");
            if (DebugModeService.IsDebugMode)
            {
                Debug.WriteLine("进入调试模式初始化...");
                ShowDebugModeWatermark();
                // 调试菜单已在服务器选择窗口打开
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

        /// <summary>
        /// 初始化导航历史服务
        /// </summary>
        private void InitializeNavigationHistory()
        {
            bool isDesktopMode = DesktopModeManager.Instance.IsDesktopMode;
            NavigationHistoryService.Instance.Initialize(this, isDesktopMode);
            NavigationHistoryService.Instance.OnNavigateBack += OnNavigateBack;
        }

        /// <summary>
        /// 返回导航事件处理
        /// </summary>
        private void OnNavigateBack(object? sender, string pageName)
        {
            // 更新侧边栏选中状态
            var button = FindMenuButtonByPageName(pageName);
            if (button != null)
            {
                SetMenuButtonActive(button);
            }

            // 导航到返回的页面（不记录历史）
            NavigateToPage(pageName, false);
        }

        /// <summary>
        /// 根据页面名称查找菜单按钮
        /// </summary>
        private Button? FindMenuButtonByPageName(string pageName)
        {
            var tag = pageName switch
            {
                "Home" => "Home",
                "Status" => "Status",
                "Logs" => "Logs",
                "Plugins" => "Plugins",
                "Files" => "Files",
                "Sandbox" => "Sandbox",
                "Database" => "Database",
                "ConfigBot" => "ConfigBot",
                "ConfigPlugin" => "ConfigPlugin",
                "ConfigUser" => "ConfigUser",
                "ConfigProtocol" => "ConfigProtocol",
                "Settings" => "Settings",
                "About" => "About",
                _ => null
            };

            if (tag == null) return null;

            // 在侧边栏中查找对应的按钮
            var sidebar = this.FindControl<StackPanel>("SidebarPanel");
            if (sidebar == null) return null;

            foreach (var child in sidebar.Children)
            {
                if (child is Button button && button.Tag?.ToString() == tag)
                {
                    return button;
                }
            }

            return null;
        }
        
        // 跟踪修饰键状态
        private bool _isCtrlPressed = false;
        private bool _isShiftPressed = false;

        /// <summary>
        /// 全局键盘事件处理
        /// </summary>
        private void OnWindowKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"KeyDown: Key={e.Key}, Modifiers={e.KeyModifiers}");

            // 首先检查 Esc 键（返回上一级）
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                if (NavigationHistoryService.Instance.HandleKeyDown(e))
                {
                    return; // 已处理返回操作
                }
            }

            // 更新修饰键状态
            if (e.Key == Avalonia.Input.Key.LeftCtrl || e.Key == Avalonia.Input.Key.RightCtrl)
            {
                _isCtrlPressed = true;
            }
            if (e.Key == Avalonia.Input.Key.LeftShift || e.Key == Avalonia.Input.Key.RightShift)
            {
                _isShiftPressed = true;
            }

            // 检测 Ctrl+Alt 组合键（用于调试模式）
            bool hasCtrl = _isCtrlPressed || (e.KeyModifiers & Avalonia.Input.KeyModifiers.Control) == Avalonia.Input.KeyModifiers.Control;
            bool hasAlt = (e.KeyModifiers & Avalonia.Input.KeyModifiers.Alt) == Avalonia.Input.KeyModifiers.Alt;
            
            if (hasCtrl && hasAlt)
            {
                DebugModeService.SetCtrlAltPressed(true);
            }

            // 检查是否按下 Ctrl+Shift+L（同时检查状态变量和 KeyModifiers）
            bool hasShift = _isShiftPressed || (e.KeyModifiers & Avalonia.Input.KeyModifiers.Shift) == Avalonia.Input.KeyModifiers.Shift;

            if (e.Key == Avalonia.Input.Key.L && hasCtrl && hasShift)
            {
                System.Diagnostics.Debug.WriteLine("快捷键 Ctrl+Shift+L 被触发");
                
                // 检查用户是否有日志权限（如果 routes 包含 "Logs"，则隐藏日志功能）
                if (_userRoutes.Contains("Logs"))
                {
                    System.Diagnostics.Debug.WriteLine("日志功能已被隐藏，快捷键不生效");
                    e.Handled = true;
                    return;
                }
                
                // 打开日志独立窗口
                OpenLogWindow();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 全局键盘释放事件处理
        /// </summary>
        private void OnWindowKeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            // 更新修饰键状态
            if (e.Key == Avalonia.Input.Key.LeftCtrl || e.Key == Avalonia.Input.Key.RightCtrl)
            {
                _isCtrlPressed = false;
            }
            if (e.Key == Avalonia.Input.Key.LeftShift || e.Key == Avalonia.Input.Key.RightShift)
            {
                _isShiftPressed = false;
            }
            
            // 检测 Ctrl+Alt 释放（用于调试模式）
            if (e.Key == Avalonia.Input.Key.LeftCtrl || e.Key == Avalonia.Input.Key.RightCtrl ||
                e.Key == Avalonia.Input.Key.LeftAlt || e.Key == Avalonia.Input.Key.RightAlt)
            {
                var isCtrlPressed = (e.KeyModifiers & Avalonia.Input.KeyModifiers.Control) == Avalonia.Input.KeyModifiers.Control;
                var isAltPressed = (e.KeyModifiers & Avalonia.Input.KeyModifiers.Alt) == Avalonia.Input.KeyModifiers.Alt;
                
                DebugModeService.SetCtrlAltPressed(isCtrlPressed && isAltPressed);
            }
        }
        
        /// <summary>
        /// 打开日志独立窗口
        /// </summary>
        private void OpenLogWindow()
        {
            // 使用 LogsPage 的静态方法打开独立窗口，传入当前窗口作为父窗口
            Pages.LogsPage.OpenLogWindowStatic(this);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // 订阅NavigationView的SelectionChanged事件
            var navigationView = this.FindControl<FluentAvalonia.UI.Controls.NavigationView>("NavigationView");
            if (navigationView != null)
            {
                navigationView.SelectionChanged += NavigationView_SelectionChanged;
            }
        }
        
        /// <summary>
        /// NavigationView选择改变事件处理
        /// </summary>
        private void NavigationView_SelectionChanged(object? sender, FluentAvalonia.UI.Controls.NavigationViewSelectionChangedEventArgs e)
        {
            if (e.SelectedItem is FluentAvalonia.UI.Controls.NavigationViewItem item && item.Tag is string tag)
            {
                Services.AppDebugLogger.LogComponentInteraction("NavigationView", "SelectionChanged", $"Tag: {tag}");

                NavigateToPage(tag);

                // 更新窗口标题为当前页面标题
                string pageTitle = tag;
                if (item.Content is TextBlock textBlock)
                {
                    pageTitle = textBlock.Text ?? tag;
                }
                else if (item.Content != null)
                {
                    pageTitle = item.Content.ToString() ?? tag;
                }
                this.Title = $"Micro Panel - {pageTitle}";

                Services.AppDebugLogger.LogStateChange("WindowTitle", null, $"Micro Panel - {pageTitle}");
            }
        }

        private async Task OnWindowLoadedAsync(object? sender, RoutedEventArgs e)
        {
            // 更新用户信息显示
            await UpdateUserInfoAsync();

            // 根据用户路由权限隐藏菜单项
            ApplyRoutePermissions();

            // 初始化导航历史服务
            InitializeNavigationHistory();

            // 默认显示首页
            NavigateToPage("Home");

            // 设置首页按钮为选中状态
            var homeButton = this.FindControl<Button>("HomeMenuButton");
            if (homeButton != null)
            {
                SetMenuButtonActive(homeButton);
            }
        }

        /// <summary>
        /// 根据用户路由权限隐藏菜单项
        /// </summary>
        private void ApplyRoutePermissions()
        {
            var session = SessionService.Instance;
            var userRoutes = session.UserRoutes;

            if (userRoutes == null || userRoutes.Count == 0)
            {
                Debug.WriteLine("[RoutePermission] 用户没有路由限制，显示所有菜单");
                return;
            }

            Debug.WriteLine($"[RoutePermission] 用户路由: {string.Join(", ", userRoutes)}");

            // 遍历 NavigationView 的所有菜单项
            var navigationView = this.FindControl<FluentAvalonia.UI.Controls.NavigationView>("NavigationView");
            if (navigationView == null)
            {
                Debug.WriteLine("[RoutePermission] 未找到 NavigationView");
                return;
            }

            // 遍历所有 MenuItems 和 FooterMenuItems
            ApplyRoutePermissionsToItems(navigationView.MenuItems, userRoutes);
            ApplyRoutePermissionsToItems(navigationView.FooterMenuItems, userRoutes);

            Debug.WriteLine("[RoutePermission] 路由权限应用完成");
        }

        /// <summary>
        /// 根据路由权限显示/隐藏菜单项
        /// routes 列表中的项是要隐藏的，其他全部显示
        /// </summary>
        private void ApplyRoutePermissionsToItems(
            IList<object>? items,
            List<string> userRoutes)
        {
            if (items == null) return;

            // 后端路由到前端 Tag 的映射
            var routeToTagMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Plugins"] = new[] { "Plugins" },           // 插件开发
                ["Permission"] = new[] { "ConfigUser" },     // 权限配置
                ["Plugin"] = new[] { "ConfigPlugin" },       // 插件配置
                ["Bot"] = new[] { "ConfigBot" },             // Bot配置
                ["Fs"] = new[] { "Files" },                  // 文件管理
            };

            foreach (var item in items.ToList())
            {
                if (item is FluentAvalonia.UI.Controls.NavigationViewItem navItem && navItem.Tag is string tag)
                {
                    // 检查该 Tag 是否在隐藏列表中
                    bool shouldHide = false;
                    
                    // 直接匹配
                    if (userRoutes.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    {
                        shouldHide = true;
                    }
                    else
                    {
                        // 通过映射表检查
                        foreach (var route in userRoutes)
                        {
                            if (routeToTagMap.TryGetValue(route, out var tags))
                            {
                                if (tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                                {
                                    shouldHide = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (shouldHide)
                    {
                        Debug.WriteLine($"[RoutePermission] 隐藏菜单项: {tag} (在隐藏列表中)");
                        navItem.IsVisible = false;
                    }
                    else
                    {
                        Debug.WriteLine($"[RoutePermission] 显示菜单项: {tag} (不在隐藏列表中)");
                        navItem.IsVisible = true;
                    }
                }
            }
        }

        private async Task UpdateUserInfoAsync()
        {
            var session = SessionService.Instance;
            if (session.CurrentUser == null || session.CurrentServer == null) return;

            // 获取用户信息并检查权限
            var userInfo = await GetUserInfoWithPermissionCheckAsync(session);
            if (userInfo == null) return; // 处理失败，已跳转

            var usernameText = this.FindControl<TextBlock>("UsernameText");
            var serverAddressText = this.FindControl<TextBlock>("ServerAddressText");
            var avatarImage = this.FindControl<Image>("UserAvatarImage");
            var avatarText = this.FindControl<TextBlock>("UserAvatarText");

            if (usernameText != null)
                usernameText.Text = session.CurrentUser.Username;

            if (serverAddressText != null)
                serverAddressText.Text = session.CurrentServer.ServerAddress;

            // 加载头像
            if (avatarImage != null)
            {
                var avatarBitmap = await LoadAvatarAsync(session);
                if (avatarBitmap != null)
                {
                    avatarImage.Source = avatarBitmap;
                    avatarImage.IsVisible = true;
                    if (avatarText != null) avatarText.IsVisible = false;
                }
                else
                {
                    avatarImage.IsVisible = false;
                    if (avatarText != null)
                    {
                        avatarText.IsVisible = true;
                        avatarText.Text = session.CurrentUser.Username[0].ToString().ToUpper();
                    }
                }
            }

            // 根据权限隐藏侧边栏模块
            ApplyPermissionHiding();
        }

        /// <summary>
        /// 获取用户信息并处理权限检查
        /// </summary>
        private async Task<UserInfoResponse?> GetUserInfoWithPermissionCheckAsync(SessionService session)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", session.Token);
                var response = await _httpClient.GetAsync($"{session.CurrentServer?.ServerAddress}/api/user/info");
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<UserInfoResponse>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // 处理 403 错误
                if (result?.Code == 403)
                {
                    var message = result.Message?.ToLower() ?? "";
                    
                    if (message.Contains("账户") || message.Contains("密码") || message.Contains("不对") || message.Contains("过期"))
                    {
                        // 账号过期，关闭窗口（服务器选择窗口会自动打开）
                        HandleAccountExpired();
                    }
                    else if (message.Contains("未找到") || message.Contains("用户") || message.Contains("登录"))
                    {
                        // 未找到用户登录，重新登录
                        await HandleReLoginAsync(session);
                    }
                    
                    return null;
                }

                // 保存用户权限路由
                if (result?.Data?.Routes != null)
                {
                    _userRoutes = result.Data.Routes;
                }

                return result?.Data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainAppWindow] GetUserInfo error: {ex}");
                return null;
            }
        }

        /// <summary>
        /// 处理账号过期
        /// </summary>
        private void HandleAccountExpired()
        {
            // 直接关闭窗口，服务器选择窗口会自动打开
            this.Close();
        }

        /// <summary>
        /// 处理重新登录
        /// </summary>
        private async Task HandleReLoginAsync(SessionService session)
        {
            try
            {
                // 使用账号密码重新登录
                var loginData = new { 
                    username = session.CurrentUser?.Username, 
                    password = session.CurrentUser?.Password 
                };
                var content = new StringContent(
                    JsonSerializer.Serialize(loginData),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{session.CurrentServer?.ServerAddress}/api/login",
                    content);
                
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.IsSuccess == true && !string.IsNullOrEmpty(result.Data?.Token))
                {
                    // 更新 Token
                    session.UpdateToken(result.Data.Token);
                    // 重新获取用户信息
                    await UpdateUserInfoAsync();
                }
                else
                {
                    // 重新登录失败，返回登录界面
                    HandleAccountExpired();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainAppWindow] Re-login error: {ex}");
                HandleAccountExpired();
            }
        }

        /// <summary>
        /// 根据权限隐藏侧边栏模块
        /// </summary>
        private void ApplyPermissionHiding()
        {
            // 获取菜单容器
            var menuStackPanel = this.FindControl<StackPanel>("MenuStackPanel");
            if (menuStackPanel == null) return;

            // 遍历所有 Expander（分组）
            foreach (var expander in menuStackPanel.Children.OfType<Expander>())
            {
                // 获取 Expander 内容中的按钮
                var contentStackPanel = expander.Content as StackPanel;
                if (contentStackPanel == null) continue;

                var childButtons = contentStackPanel.Children.OfType<Button>().Where(b => b.Tag != null).ToList();
                
                // 遍历按钮检查权限
                foreach (var button in childButtons)
                {
                    var tag = button.Tag?.ToString();
                    if (string.IsNullOrEmpty(tag)) continue;

                    // 检查该按钮是否需要隐藏
                    if (_tagToRouteMap.TryGetValue(tag, out var routeName))
                    {
                        // 如果 routes 包含该路由名称，则隐藏
                        if (_userRoutes.Contains(routeName))
                        {
                            button.IsVisible = false;
                        }
                    }
                }

                // 检查该 Expander 是否需要隐藏（如果内部所有按钮都被隐藏）
                var allHidden = childButtons.All(b => !b.IsVisible);
                expander.IsVisible = !allHidden;
            }

            // 处理独立的按钮（如首页）
            var directButtons = menuStackPanel.Children.OfType<Button>().Where(b => b.Tag != null);
            foreach (var button in directButtons)
            {
                var tag = button.Tag?.ToString();
                if (string.IsNullOrEmpty(tag)) continue;

                if (_tagToRouteMap.TryGetValue(tag, out var routeName))
                {
                    if (_userRoutes.Contains(routeName))
                    {
                        button.IsVisible = false;
                    }
                }
            }
        }

        private async Task<Bitmap?> LoadAvatarAsync(SessionService session)
        {
            try
            {
                // 获取用户信息
                _httpClient.DefaultRequestHeaders.Remove("token");
                _httpClient.DefaultRequestHeaders.Add("token", session.Token);
                var response = await _httpClient.GetAsync($"{session.CurrentServer?.ServerAddress}/api/user/info");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiResponse<UserStore>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    string? avatarUrl = null;
                    if (!string.IsNullOrEmpty(result?.Data?.Avatar))
                    {
                        avatarUrl = result.Data.Avatar;
                    }
                    else if (!string.IsNullOrEmpty(result?.Data?.MasterQQ))
                    {
                        // 使用QQ头像API
                        avatarUrl = $"https://q1.qlogo.cn/g?b=qq&s=0&nk={result.Data.MasterQQ}";
                    }

                    if (!string.IsNullOrEmpty(avatarUrl))
                    {
                        var imgResponse = await _httpClient.GetAsync(avatarUrl);
                        if (imgResponse.IsSuccessStatusCode)
                        {
                            var stream = await imgResponse.Content.ReadAsStreamAsync();
                            return new Bitmap(stream);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 头像加载失败，返回null显示默认文字
            }
            return null;
        }

        public class UserStore
        {
            public string? Username { get; set; }
            public string? Avatar { get; set; }
            public string? MasterQQ { get; set; }
        }

        private void OnMenuClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var tag = button.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag))
                {
                    NavigateToPage(tag);
                    SetMenuButtonActive(button);
                }
            }
        }

        private void SetMenuButtonActive(Button button)
        {
            // 重置之前选中的按钮
            if (_currentMenuButton != null)
            {
                _currentMenuButton.Classes.Remove("Selected");
            }

            // 设置当前按钮为选中状态
            button.Classes.Add("Selected");
            _currentMenuButton = button;

            // 更新滑动蓝条位置
            UpdateSelectionIndicator(button);
        }

        /// <summary>
        /// 更新选中指示器（蓝条）位置，带动画效果
        /// </summary>
        private void UpdateSelectionIndicator(Button targetButton)
        {
            var indicator = this.FindControl<Border>("SelectionIndicator");
            if (indicator == null) return;

            // 显示指示器
            indicator.IsVisible = true;

            // 使用 Dispatcher 确保布局已完成
            Dispatcher.UIThread.Post(() =>
            {
                // 获取按钮相对于侧边栏 Grid 的位置
                var sidebarGrid = this.FindControl<Grid>("SidebarGrid");
                if (sidebarGrid == null) return;

                // 将按钮的坐标转换为相对于 Grid 的坐标
                var buttonPosition = targetButton.TranslatePoint(new Point(0, 0), sidebarGrid);
                if (!buttonPosition.HasValue) return;

                // 获取按钮高度
                double buttonHeight = targetButton.Bounds.Height;
                if (buttonHeight <= 0) buttonHeight = 40;

                // 获取指示器高度
                double indicatorHeight = indicator.Bounds.Height;
                if (indicatorHeight <= 0) indicatorHeight = 16;

                // 计算垂直居中位置
                // 注意：buttonPosition 是相对于整个 Grid 的，但 Canvas 在 Row 1 中
                // 所以需要减去 Row 0 (Logo区域) 的高度
                double logoHeight = 60; // Logo区域大约高度
                double centerY = buttonPosition.Value.Y - logoHeight + (buttonHeight - indicatorHeight) / 2;

                // 设置 Canvas.Top 属性来移动指示器
                Canvas.SetTop(indicator, centerY);

            }, DispatcherPriority.Render);
        }

        private async void NavigateToPage(string pageName, bool recordHistory = true)
        {
            var contentControl = this.FindControl<ContentControl>("MainContentControl");
            var titleText = this.FindControl<TextBlock>("PageTitleText");

            if (contentControl == null) return;

            // 记录导航前的页面
            var fromPage = _currentPage?.GetType().Name ?? "None";

            Services.AppDebugLogger.LogNavigation(fromPage, pageName, recordHistory ? "记录历史" : "不记录历史");

            UserControl? newPage = pageName switch
            {
                "Home" => new HomePage(),
                "Status" => new StatusPage(),
                "Logs" => new LogsPage(),
                "Plugins" => new PluginsPage(),
                "Files" => new FileManagerPage(),
                "Sandbox" => new SandboxDebugPage(),
                "Database" => new DatabasePage(),
                "ConfigBot" => new BotConfigPage(),
                "ConfigPlugin" => new PluginConfigPage(),
                "ConfigUser" => new UserConfigPage(),
                "ConfigProtocol" => new ProtocolConfigPage(),
                "Settings" => new SettingsPage(),
                "About" => new AboutPage(),
                // 其他页面将在后续添加
                _ => null
            };

            if (newPage != null)
            {
                Services.AppDebugLogger.LogComponentInteraction("NavigationView", "Navigate", $"目标页面: {pageName}");

                // 执行页面切换动画
                await AnimatePageTransitionAsync(contentControl, newPage);

                if (titleText != null)
                {
                    titleText.Text = pageName switch
                    {
                        "Home" => "首页",
                        "Status" => "系统状态",
                        "Logs" => "日志输出",
                        "Plugins" => "插件开发",
                        "Files" => "文件管理",
                        "Sandbox" => "沙盒调试",
                        "Database" => "数据库",
                        "ConfigBot" => "Bot配置",
                        "ConfigPlugin" => "插件配置",
                        "ConfigUser" => "权限配置",
                        "ConfigProtocol" => "协议配置",
                        "Settings" => "设置",
                        "About" => "关于",
                        _ => pageName
                    };
                }

                // 记录导航历史
                if (recordHistory)
                {
                    NavigationHistoryService.Instance.Push(pageName);
                    Services.AppDebugLogger.LogStateChange("NavigationHistory", null, pageName);
                }

                Services.AppDebugLogger.LogUserAction("页面导航完成", $"当前页面: {pageName}");
            }
            else
            {
                Services.AppDebugLogger.LogComponentInteraction("NavigationView", "Navigate Failed", $"未知页面: {pageName}");
            }
        }

        /// <summary>
        /// 执行页面切换动画：上一个页面下沉渐隐，下一个页面上浮渐显
        /// </summary>
        private async Task AnimatePageTransitionAsync(ContentControl contentControl, UserControl newPage)
        {
            const double animationDuration = 250; // 毫秒

            // 如果有当前页面，先执行下沉渐隐动画
            if (_currentPage != null)
            {
                await AnimatePageExitAsync(_currentPage, animationDuration);
            }

            // 设置新页面
            _currentPage = newPage;
            contentControl.Content = newPage;

            // 执行上浮渐显动画
            await AnimatePageEnterAsync(newPage, animationDuration);
        }

        /// <summary>
        /// 页面退出动画：下沉渐隐
        /// </summary>
        private async Task AnimatePageExitAsync(UserControl page, double durationMs)
        {
            var visual = ElementComposition.GetElementVisual(page);
            if (visual == null) return;

            var compositor = visual.Compositor;

            // 创建透明度动画
            var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacityAnimation.InsertKeyFrame(0f, 1f);
            opacityAnimation.InsertKeyFrame(1f, 0f);

            // 创建位移动画 (Y轴向下移动20像素)
            var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
            offsetAnimation.Duration = TimeSpan.FromMilliseconds(durationMs);
            offsetAnimation.InsertKeyFrame(0f, new Vector3(0, 0, 0));
            offsetAnimation.InsertKeyFrame(1f, new Vector3(0, 20, 0));

            // 启动动画
            visual.StartAnimation("Opacity", opacityAnimation);
            visual.StartAnimation("Offset", offsetAnimation);

            // 等待动画完成
            await Task.Delay(TimeSpan.FromMilliseconds(durationMs));
        }

        /// <summary>
        /// 页面进入动画：上浮渐显
        /// </summary>
        private async Task AnimatePageEnterAsync(UserControl page, double durationMs)
        {
            var visual = ElementComposition.GetElementVisual(page);
            if (visual == null) return;

            var compositor = visual.Compositor;

            // 设置初始状态
            visual.Opacity = 0;
            visual.Offset = new Vector3(0, -20, 0);

            // 创建透明度动画
            var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacityAnimation.InsertKeyFrame(0f, 0f);
            opacityAnimation.InsertKeyFrame(1f, 1f);

            // 创建位移动画 (Y轴从-20移动到0)
            var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
            offsetAnimation.Duration = TimeSpan.FromMilliseconds(durationMs);
            offsetAnimation.InsertKeyFrame(0f, new Vector3(0, -20, 0));
            offsetAnimation.InsertKeyFrame(1f, new Vector3(0, 0, 0));

            // 启动动画
            visual.StartAnimation("Opacity", opacityAnimation);
            visual.StartAnimation("Offset", offsetAnimation);

            // 等待动画完成
            await Task.Delay(TimeSpan.FromMilliseconds(durationMs));
        }

        /// <summary>
        /// 导航到指定页面控件
        /// </summary>
        public void NavigateToPage(UserControl page)
        {
            var contentControl = this.FindControl<ContentControl>("MainContentControl");
            if (contentControl != null)
            {
                _currentPage = page;
                contentControl.Content = page;
            }
        }

        private async void OnLogoutClick(object? sender, RoutedEventArgs e)
        {
            // 检查是否按下了 Ctrl+Alt，如果是则进入调试模式
            if (DebugModeService.IsCtrlAltPressed)
            {
                var result = await DebugModeService.ShowDebugModeConfirmDialog(this);
                if (result)
                {
                    DebugModeService.RestartInDebugMode();
                }
                return;
            }

            // 结束会话
            SessionService.Instance.EndSession();
            
            // 关闭当前窗口，返回主窗口
            Close();
        }

    }
}
