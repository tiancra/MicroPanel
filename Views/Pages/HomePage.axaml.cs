using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using MicroPanel.Models;
using MicroPanel.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MicroPanel.Views.Pages
{
    public partial class HomePage : UserControl
    {
        private readonly AuthenticatedApiService _apiService;
        private readonly HttpClient _httpClient;

        public HomePage()
        {
            InitializeComponent();
            _apiService = new AuthenticatedApiService();
            _httpClient = new HttpClient();
            
            // 页面加载时获取数据
            Loaded += OnPageLoaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnPageLoaded(object? sender, EventArgs e)
        {
            // 页面加载时获取一次数据
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var session = SessionService.Instance;
            if (!session.IsLoggedIn)
            {
                System.Diagnostics.Debug.WriteLine("HomePage: 用户未登录");
                return;
            }

            _apiService.SetBaseUrl(session.CurrentServer?.ServerAddress ?? "");
            System.Diagnostics.Debug.WriteLine($"HomePage: 正在获取Bot信息，BaseUrl: {session.CurrentServer?.ServerAddress}");

            // 获取一言
            _ = LoadHitokotoAsync();

            // 获取 Bot 信息
            var botInfoResponse = await _apiService.GetBotInfoAsync(session.Token!);
            System.Diagnostics.Debug.WriteLine($"HomePage: Bot信息响应 Code={botInfoResponse?.Code}, Message={botInfoResponse?.Message}, DataCount={botInfoResponse?.Data?.Count ?? 0}");
            
            if (botInfoResponse?.IsSuccess == true && botInfoResponse.Data != null)
            {
                var itemsControl = this.FindControl<ItemsControl>("BotInfoItemsControl");
                var emptyPanel = this.FindControl<StackPanel>("EmptyStatePanel");

                System.Diagnostics.Debug.WriteLine($"HomePage: 设置ItemsSource，数据条数: {botInfoResponse.Data.Count}");
                
                // 转换为 ViewModel 并加载头像
                var viewModels = new List<BotInfoViewModel>();
                foreach (var botInfo in botInfoResponse.Data)
                {
                    var vm = new BotInfoViewModel(botInfo);
                    _ = vm.LoadAvatarAsync(_httpClient); // 异步加载头像
                    viewModels.Add(vm);
                }
                
                if (itemsControl != null)
                {
                    itemsControl.ItemsSource = viewModels;
                }

                if (emptyPanel != null)
                {
                    emptyPanel.IsVisible = botInfoResponse.Data.Count == 0;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"HomePage: 获取Bot信息失败 - {botInfoResponse?.Message}");
                var emptyPanel = this.FindControl<StackPanel>("EmptyStatePanel");
                if (emptyPanel != null)
                {
                    emptyPanel.IsVisible = true;
                }
            }

            // 更新欢迎信息
            await UpdateWelcomeInfoAsync();
        }

        /// <summary>
        /// 获取一言
        /// </summary>
        private async Task LoadHitokotoAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://v1.hitokoto.cn/?c=b&encode=text");
                if (response.IsSuccessStatusCode)
                {
                    var hitokoto = await response.Content.ReadAsStringAsync();
                    var hitokotoText = this.FindControl<TextBlock>("HitokotoText");
                    if (hitokotoText != null)
                    {
                        hitokotoText.Text = hitokoto.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomePage] 获取一言失败: {ex.Message}");
                var hitokotoText = this.FindControl<TextBlock>("HitokotoText");
                if (hitokotoText != null)
                {
                    hitokotoText.Text = "欢迎回来~";
                }
            }
        }

        private async Task UpdateWelcomeInfoAsync()
        {
            var session = SessionService.Instance;
            if (session.CurrentUser == null) return;

            var welcomeText = this.FindControl<TextBlock>("WelcomeText");
            var avatarImage = this.FindControl<Image>("AvatarImage");
            var avatarText = this.FindControl<TextBlock>("AvatarText");

            if (welcomeText != null)
            {
                var timeOfDay = GetTimeOfDay();
                welcomeText.Text = $"{timeOfDay}好！{session.CurrentUser.Username}";
            }

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
        }

        private async Task<Bitmap?> LoadAvatarAsync(SessionService session)
        {
            try
            {
                // 尝试从用户存储中获取头像URL
                var userStore = await GetUserStoreAsync(session);
                string? avatarUrl = null;

                if (!string.IsNullOrEmpty(userStore?.Avatar))
                {
                    avatarUrl = userStore.Avatar;
                }
                else if (!string.IsNullOrEmpty(userStore?.MasterQQ))
                {
                    // 使用QQ头像API
                    avatarUrl = $"https://q1.qlogo.cn/g?b=qq&s=0&nk={userStore.MasterQQ}";
                }

                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    var response = await _httpClient.GetAsync(avatarUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var stream = await response.Content.ReadAsStreamAsync();
                        return new Bitmap(stream);
                    }
                }
            }
            catch (Exception)
            {
                // 头像加载失败，返回null显示默认文字
            }
            return null;
        }

        private async Task<UserStore?> GetUserStoreAsync(SessionService session)
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
                    var result = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<UserStore>>(json, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result?.Data;
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        private string GetTimeOfDay()
        {
            var hour = DateTime.Now.Hour;
            return hour switch
            {
                >= 5 and < 12 => "早上",
                >= 12 and < 14 => "中午",
                >= 14 and < 18 => "下午",
                >= 18 and < 22 => "晚上",
                _ => "深夜"
            };
        }
    }

    public class UserStore
    {
        public string? Username { get; set; }
        public string? Avatar { get; set; }
        public string? MasterQQ { get; set; }
    }

    public class BotInfoViewModel : INotifyPropertyChanged
    {
        private readonly BotInfo _botInfo;
        private Bitmap? _avatarBitmap;

        public BotInfoViewModel(BotInfo botInfo)
        {
            _botInfo = botInfo;
        }

        public string? Nickname => _botInfo.Nickname;
        public string? AvatarUrl => _botInfo.AvatarUrl;
        public BotContacts? CountContacts => _botInfo.CountContacts;
        public BotMessageCount? MessageCount => _botInfo.MessageCount;
        public string? BotVersion => _botInfo.BotVersion;
        public string? Platform => _botInfo.Platform;
        public string? BotRunTime => _botInfo.BotRunTime;

        public Bitmap? AvatarBitmap
        {
            get => _avatarBitmap;
            set
            {
                _avatarBitmap = value;
                OnPropertyChanged(nameof(AvatarBitmap));
                OnPropertyChanged(nameof(IsAvatarVisible));
                OnPropertyChanged(nameof(IsTextAvatarVisible));
            }
        }

        public bool IsAvatarVisible => _avatarBitmap != null;
        public bool IsTextAvatarVisible => _avatarBitmap == null;

        public async Task LoadAvatarAsync(HttpClient httpClient)
        {
            if (string.IsNullOrEmpty(_botInfo.AvatarUrl))
                return;

            try
            {
                var response = await httpClient.GetAsync(_botInfo.AvatarUrl);
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    AvatarBitmap = new Bitmap(stream);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载Bot头像失败: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
