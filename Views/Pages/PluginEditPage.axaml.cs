using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using MicroPanel.Models;
using MicroPanel.Services;
using MicroPanel.Views;
using MicroPanel.Views.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MicroPanel.Views.Pages
{
    public partial class PluginEditPage : UserControl
    {
        private readonly PluginService _pluginService;
        private PluginType _pluginData = new();
        private ObservableCollection<string> _groups = new();
        private ObservableCollection<string> _friends = new();
        private string _editMode = "add"; // "add" or "update"

        public PluginEditPage()
        {
            InitializeComponent();
            _pluginService = new PluginService();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // 初始化控件引用
            var flagComboBox = this.FindControl<ComboBox>("FlagComboBox");
            var messageTypeComboBox = this.FindControl<ComboBox>("MessageTypeComboBox");
            var groupsItemsControl = this.FindControl<ItemsControl>("GroupsItemsControl");
            var friendsItemsControl = this.FindControl<ItemsControl>("FriendsItemsControl");

            // 设置标志选项
            if (flagComboBox != null)
            {
                flagComboBox.ItemsSource = PluginService.GetRegexFlagOptions();
                flagComboBox.DisplayMemberBinding = new Avalonia.Data.Binding("Description");
                flagComboBox.SelectedValueBinding = new Avalonia.Data.Binding("Value");
            }

            // 设置消息类型选项
            if (messageTypeComboBox != null)
            {
                messageTypeComboBox.ItemsSource = PluginService.GetMessageSegmentOptions();
                messageTypeComboBox.DisplayMemberBinding = new Avalonia.Data.Binding("Name");
            }

            if (groupsItemsControl != null)
            {
                groupsItemsControl.ItemsSource = _groups;
            }

            if (friendsItemsControl != null)
            {
                friendsItemsControl.ItemsSource = _friends;
            }
        }

        /// <summary>
        /// 设置插件数据
        /// </summary>
        public async void SetPlugin(PluginType? plugin, string mode)
        {
            _editMode = mode;

            if (mode == "add")
            {
                _pluginData = new PluginType
                {
                    Id = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Event = "message",
                    IsGlobal = true
                };
                LoadPluginData();
            }
            else if (plugin != null)
            {
                _pluginData = plugin;
                
                // 如果是更新模式且消息包含 button、markdown 或 code 类型，需要调用 btnJSON 获取完整数据
                if (mode == "update" && plugin.Message != null && 
                    plugin.Message.Any(m => m.Type == "button" || m.Type == "markdown" || m.Type == "code"))
                {
                    await LoadPluginDataWithBtnJsonAsync();
                }
                else
                {
                    LoadPluginData();
                }
            }
        }

        /// <summary>
        /// 调用 btnJSON 接口加载完整插件数据
        /// </summary>
        private async Task LoadPluginDataWithBtnJsonAsync()
        {
            try
            {
                var response = await _pluginService.GetButtonJsonAsync(_pluginData);
                if (response?.Code == 200 && response.Data != null)
                {
                    // 更新插件数据
                    _pluginData = response.Data;
                    if (response.Data.Message != null)
                    {
                        _pluginData.Message = response.Data.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PluginEditPage: 加载按钮JSON失败 - {ex.Message}");
            }
            
            LoadPluginData();
        }

        /// <summary>
        /// 加载插件数据到UI
        /// </summary>
        private void LoadPluginData()
        {
            var cronTextBox = this.FindControl<TextBox>("CronTextBox");
            var regTextBox = this.FindControl<TextBox>("RegTextBox");
            var flagComboBox = this.FindControl<ComboBox>("FlagComboBox");
            var delayNumeric = this.FindControl<NumericUpDown>("DelayNumeric");
            var isAtToggle = this.FindControl<ToggleSwitch>("IsAtToggle");
            var isQuoteToggle = this.FindControl<ToggleSwitch>("IsQuoteToggle");
            var isGlobalToggle = this.FindControl<ToggleSwitch>("IsGlobalToggle");

            if (cronTextBox != null) cronTextBox.Text = _pluginData.Cron;
            if (regTextBox != null) regTextBox.Text = _pluginData.Reg;
            if (delayNumeric != null) delayNumeric.Value = _pluginData.DelayTime;
            if (isAtToggle != null) isAtToggle.IsChecked = _pluginData.IsAt;
            if (isQuoteToggle != null) isQuoteToggle.IsChecked = _pluginData.IsQuote;
            if (isGlobalToggle != null) isGlobalToggle.IsChecked = _pluginData.IsGlobal;

            // 设置标志
            if (flagComboBox != null)
            {
                var options = PluginService.GetRegexFlagOptions();
                flagComboBox.SelectedItem = options.FirstOrDefault(o => o.Value == _pluginData.Flag);
            }

            // 更新定时任务启用状态
            UpdateCronEnabled();

            // 加载群组
            _groups.Clear();
            foreach (var group in _pluginData.Groups)
            {
                _groups.Add(group);
            }

            // 加载好友
            _friends.Clear();
            foreach (var friend in _pluginData.Friends)
            {
                _friends.Add(friend);
            }

            // 加载消息段
            LoadMessageSegments();

            UpdateLabels();
        }

        /// <summary>
        /// 获取消息类型名称
        /// </summary>
        private string GetMessageTypeName(string type)
        {
            return type switch
            {
                "text" => "文本",
                "image" => "图片",
                "record" => "音频",
                "video" => "视频",
                "face" => "表情",
                "poke" => "戳一戳",
                "dice" => "骰子",
                "rps" => "猜拳",
                "markdown" => "Markdown",
                "button" => "按钮",
                "code" => "代码",
                _ => type
            };
        }

        /// <summary>
        /// 全局触发开关变化
        /// </summary>
        private void OnIsGlobalChanged(object? sender, RoutedEventArgs e)
        {
            UpdateLabels();
            UpdateCronEnabled();
        }

        /// <summary>
        /// 更新定时任务输入框的启用状态
        /// </summary>
        private void UpdateCronEnabled()
        {
            var cronTextBox = this.FindControl<TextBox>("CronTextBox");
            var isGlobalToggle = this.FindControl<ToggleSwitch>("IsGlobalToggle");
            
            if (cronTextBox != null && isGlobalToggle != null)
            {
                cronTextBox.IsEnabled = !isGlobalToggle.IsChecked.GetValueOrDefault(true);
            }
        }

        /// <summary>
        /// 加载消息段编辑器
        /// </summary>
        private void LoadMessageSegments()
        {
            var stackPanel = this.FindControl<StackPanel>("MessageSegmentsStackPanel");
            if (stackPanel == null) return;

            stackPanel.Children.Clear();

            for (int i = 0; i < _pluginData.Message.Count; i++)
            {
                var msg = _pluginData.Message[i];
                var editor = new MessageSegmentEditor();
                editor.SetMessage(msg, i);
                editor.DeleteRequested += OnMessageSegmentDeleteRequested;
                editor.DataChanged += OnMessageSegmentDataChanged;
                stackPanel.Children.Add(editor);
            }
        }

        /// <summary>
        /// 添加消息段编辑器
        /// </summary>
        private void AddMessageSegmentEditor(MessageType message)
        {
            var stackPanel = this.FindControl<StackPanel>("MessageSegmentsStackPanel");
            if (stackPanel == null) return;

            var index = stackPanel.Children.Count;
            var editor = new MessageSegmentEditor();
            editor.SetMessage(message, index);
            editor.DeleteRequested += OnMessageSegmentDeleteRequested;
            editor.DataChanged += OnMessageSegmentDataChanged;
            stackPanel.Children.Add(editor);
        }

        /// <summary>
        /// 消息段删除请求
        /// </summary>
        private void OnMessageSegmentDeleteRequested(object? sender, int index)
        {
            var stackPanel = this.FindControl<StackPanel>("MessageSegmentsStackPanel");
            if (stackPanel == null) return;

            // 移除指定索引的编辑器
            if (index >= 0 && index < stackPanel.Children.Count)
            {
                stackPanel.Children.RemoveAt(index);
                
                // 重新设置索引
                for (int i = 0; i < stackPanel.Children.Count; i++)
                {
                    if (stackPanel.Children[i] is MessageSegmentEditor editor)
                    {
                        var msg = editor.GetMessage();
                        editor.SetMessage(msg, i);
                    }
                }
            }
        }

        /// <summary>
        /// 消息段数据变化
        /// </summary>
        private void OnMessageSegmentDataChanged(object? sender, MessageType message)
        {
            // 数据已更新，不需要额外处理
        }

        /// <summary>
        /// 更新标签文本
        /// </summary>
        private void UpdateLabels()
        {
            var isGlobalToggle = this.FindControl<ToggleSwitch>("IsGlobalToggle");
            var groupsLabel = this.FindControl<TextBlock>("GroupsLabel");
            var friendsLabel = this.FindControl<TextBlock>("FriendsLabel");

            bool isGlobal = isGlobalToggle?.IsChecked ?? true;

            if (groupsLabel != null)
                groupsLabel.Text = isGlobal ? "黑名单群" : "白名单群";

            if (friendsLabel != null)
                friendsLabel.Text = isGlobal ? "黑名单用户" : "白名单用户";
        }

        /// <summary>
        /// 添加群组
        /// </summary>
        private void OnAddGroupClick(object? sender, RoutedEventArgs e)
        {
            var groupTextBox = this.FindControl<TextBox>("GroupTextBox");
            if (groupTextBox != null && !string.IsNullOrWhiteSpace(groupTextBox.Text))
            {
                _groups.Add(groupTextBox.Text.Trim());
                groupTextBox.Text = "";
            }
        }

        /// <summary>
        /// 移除群组
        /// </summary>
        private void OnRemoveGroupClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string group)
            {
                _groups.Remove(group);
            }
        }

        /// <summary>
        /// 添加好友
        /// </summary>
        private void OnAddFriendClick(object? sender, RoutedEventArgs e)
        {
            var friendTextBox = this.FindControl<TextBox>("FriendTextBox");
            if (friendTextBox != null && !string.IsNullOrWhiteSpace(friendTextBox.Text))
            {
                _friends.Add(friendTextBox.Text.Trim());
                friendTextBox.Text = "";
            }
        }

        /// <summary>
        /// 移除好友
        /// </summary>
        private void OnRemoveFriendClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string friend)
            {
                _friends.Remove(friend);
            }
        }

        /// <summary>
        /// 消息类型选择变化
        /// </summary>
        private void OnMessageTypeChanged(object? sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is MessageSegmentOption option)
            {
                // 检查是否可以添加该类型
                if (!CanAddMessageType(option.Type))
                {
                    ShowError("无法添加此消息类型，与现有消息类型冲突！");
                    comboBox.SelectedIndex = -1;
                    return;
                }

                // 创建新的消息段
                var newMessage = new MessageType
                {
                    Type = option.Type,
                    Data = option.DefaultValue.Data,
                    Url = option.DefaultValue.Url,
                    Hash = option.DefaultValue.Hash
                };

                _pluginData.Message.Add(newMessage);
                AddMessageSegmentEditor(newMessage);

                comboBox.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// 检查是否可以添加消息类型
        /// </summary>
        private bool CanAddMessageType(string type)
        {
            // 文本、图片、表情可以与同类共存
            var compatibleTypes = new[] { "text", "image", "face" };
            
            // 音频、视频只能单独发送
            var exclusiveTypes = new[] { "record", "video" };

            var stackPanel = this.FindControl<StackPanel>("MessageSegmentsStackPanel");
            var count = stackPanel?.Children.Count ?? 0;

            if (exclusiveTypes.Contains(type))
            {
                return count == 0;
            }

            if (count > 0)
            {
                var existingTypes = new List<string>();
                foreach (var child in stackPanel!.Children)
                {
                    if (child is MessageSegmentEditor editor)
                    {
                        existingTypes.Add(editor.GetMessage().Type);
                    }
                }
                
                // 如果已有独占类型，不能添加其他类型
                if (existingTypes.Any(t => exclusiveTypes.Contains(t)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 返回
        /// </summary>
        private void OnGoBackClick(object? sender, RoutedEventArgs e)
        {
            var mainWindow = TopLevel.GetTopLevel(this) as MainAppWindow;
            if (mainWindow != null)
            {
                mainWindow.NavigateToPage(new PluginsPage());
            }
        }

        /// <summary>
        /// 保存插件
        /// </summary>
        private async void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            // 收集数据
            var cronTextBox = this.FindControl<TextBox>("CronTextBox");
            var regTextBox = this.FindControl<TextBox>("RegTextBox");
            var flagComboBox = this.FindControl<ComboBox>("FlagComboBox");
            var delayNumeric = this.FindControl<NumericUpDown>("DelayNumeric");
            var isAtToggle = this.FindControl<ToggleSwitch>("IsAtToggle");
            var isQuoteToggle = this.FindControl<ToggleSwitch>("IsQuoteToggle");
            var isGlobalToggle = this.FindControl<ToggleSwitch>("IsGlobalToggle");

            _pluginData.Cron = cronTextBox?.Text ?? "";
            _pluginData.Reg = regTextBox?.Text ?? "";
            _pluginData.DelayTime = (int)(delayNumeric?.Value ?? 0);
            _pluginData.IsAt = isAtToggle?.IsChecked ?? false;
            _pluginData.IsQuote = isQuoteToggle?.IsChecked ?? false;
            _pluginData.IsGlobal = isGlobalToggle?.IsChecked ?? true;

            if (flagComboBox?.SelectedItem is RegexFlagOption flagOption)
            {
                _pluginData.Flag = flagOption.Value;
            }

            _pluginData.Groups = _groups.ToList();
            _pluginData.Friends = _friends.ToList();
            
            // 从编辑器收集消息数据
            _pluginData.Message.Clear();
            var stackPanel = this.FindControl<StackPanel>("MessageSegmentsStackPanel");
            if (stackPanel != null)
            {
                foreach (var child in stackPanel.Children)
                {
                    if (child is MessageSegmentEditor editor)
                    {
                        _pluginData.Message.Add(editor.GetMessage());
                    }
                }
            }

            // 验证
            if (_pluginData.Message.Count == 0)
            {
                ShowError("消息段内容不能为空，请至少添加一种消息类型！");
                return;
            }

            // 保存
            PluginListResponse? response;
            if (_editMode == "add")
            {
                response = await _pluginService.AddPluginAsync(_pluginData);
            }
            else
            {
                response = await _pluginService.UpdatePluginAsync(_pluginData.Id, _pluginData);
            }

            if (response?.Code == 200)
            {
                // 保存成功，返回列表页
                var mainWindow = TopLevel.GetTopLevel(this) as MainAppWindow;
                if (mainWindow != null)
                {
                    mainWindow.NavigateToPage(new PluginsPage());
                }
            }
            else
            {
                ShowError(response?.Message ?? "保存失败");
            }
        }

        /// <summary>
        /// 显示错误信息
        /// </summary>
        private async void ShowError(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "错误",
                Content = message,
                CloseButtonText = "确定"
            };
            await dialog.ShowAsync();
        }
    }
}
