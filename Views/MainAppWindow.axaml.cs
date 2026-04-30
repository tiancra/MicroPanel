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
using MicroPanelAvalonia.Models;
using MicroPanelAvalonia.Services;
using MicroPanelAvalonia.Views.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;

namespace MicroPanelAvalonia.Views
{
    public partial class MainAppWindow : Window
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
            
            // 窗口加载时初始化
            Loaded += async (s, e) => await OnWindowLoadedAsync(s, e);
            
            // 注册全局键盘事件
            KeyDown += OnWindowKeyDown;
            KeyUp += OnWindowKeyUp;
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

            // 更新修饰键状态
            if (e.Key == Avalonia.Input.Key.LeftCtrl || e.Key == Avalonia.Input.Key.RightCtrl)
            {
                _isCtrlPressed = true;
            }
            if (e.Key == Avalonia.Input.Key.LeftShift || e.Key == Avalonia.Input.Key.RightShift)
            {
                _isShiftPressed = true;
            }

            // 检查是否按下 Ctrl+Shift+L（同时检查状态变量和 KeyModifiers）
            bool hasCtrl = _isCtrlPressed || (e.KeyModifiers & Avalonia.Input.KeyModifiers.Control) == Avalonia.Input.KeyModifiers.Control;
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
        }

        private async Task OnWindowLoadedAsync(object? sender, RoutedEventArgs e)
        {
            // 更新用户信息显示
            await UpdateUserInfoAsync();
            
            // 默认显示首页
            NavigateToPage("Home");
            
            // 设置首页按钮为选中状态
            var homeButton = this.FindControl<Button>("HomeMenuButton");
            if (homeButton != null)
            {
                SetMenuButtonActive(homeButton);
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
                _currentMenuButton.Classes.Remove("Accent");
            }

            // 设置当前按钮为选中状态
            button.Classes.Add("Accent");
            _currentMenuButton = button;
        }

        private async void NavigateToPage(string pageName)
        {
            var contentControl = this.FindControl<ContentControl>("MainContentControl");
            var titleText = this.FindControl<TextBlock>("PageTitleText");

            if (contentControl == null) return;

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
                "About" => new AboutPage(),
                // 其他页面将在后续添加
                _ => null
            };

            if (newPage != null)
            {
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
                        _ => pageName
                    };
                }
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

        private void OnLogoutClick(object? sender, RoutedEventArgs e)
        {
            // 结束会话
            SessionService.Instance.EndSession();
            
            // 关闭当前窗口，返回主窗口
            Close();
        }
    }
}
