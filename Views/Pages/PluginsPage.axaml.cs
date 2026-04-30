using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using MicroPanelAvalonia.Models;
using MicroPanelAvalonia.Services;
using MicroPanelAvalonia.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MicroPanelAvalonia.Views.Pages
{
    public partial class PluginsPage : UserControl
    {
        private readonly PluginService _pluginService;
        private ObservableCollection<PluginViewModel> _pluginsList = new();
        private List<PluginViewModel> _pluginsListCopy = new();

        public PluginsPage()
        {
            InitializeComponent();
            _pluginService = new PluginService();
            
            Loaded += OnLoaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            var itemsControl = this.FindControl<ItemsControl>("PluginsItemsControl");
            if (itemsControl != null)
            {
                itemsControl.ItemsSource = _pluginsList;
            }
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await LoadPluginsAsync();
        }

        /// <summary>
        /// 加载插件列表
        /// </summary>
        private async Task LoadPluginsAsync()
        {
            var response = await _pluginService.GetPluginsListAsync();
            if (response?.Code == 200 && response.Data != null)
            {
                _pluginsList.Clear();
                for (int i = 0; i < response.Data.Count; i++)
                {
                    _pluginsList.Add(new PluginViewModel
                    {
                        Index = i,
                        Plugin = response.Data[i],
                        MessageTypes = GetMessageTypesString(response.Data[i].Message)
                    });
                }
                _pluginsListCopy = _pluginsList.ToList();
            }
        }

        /// <summary>
        /// 获取消息类型字符串
        /// </summary>
        private string GetMessageTypesString(List<MessageType> messages)
        {
            if (messages == null || messages.Count == 0)
                return "[]";
            
            var types = messages.Select(m => m.Type).ToList();
            return $"[{string.Join(", ", types)}]";
        }

        /// <summary>
        /// 搜索文本变化
        /// </summary>
        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            var searchBox = sender as TextBox;
            var searchText = searchBox?.Text?.ToLower() ?? "";

            if (string.IsNullOrEmpty(searchText))
            {
                _pluginsList.Clear();
                foreach (var plugin in _pluginsListCopy)
                {
                    _pluginsList.Add(plugin);
                }
            }
            else
            {
                var filtered = _pluginsListCopy.Where(p => 
                    p.Plugin.Reg.ToLower().Contains(searchText)).ToList();
                
                _pluginsList.Clear();
                foreach (var plugin in filtered)
                {
                    _pluginsList.Add(plugin);
                }
            }
        }

        /// <summary>
        /// 添加插件
        /// </summary>
        private void OnAddPluginClick(object? sender, RoutedEventArgs e)
        {
            // 导航到插件编辑页面，添加模式
            var mainWindow = TopLevel.GetTopLevel(this) as MainAppWindow;
            if (mainWindow != null)
            {
                var editPage = new PluginEditPage();
                editPage.SetPlugin(null, "add");
                mainWindow.NavigateToPage(editPage);
            }
        }

        /// <summary>
        /// 编辑插件
        /// </summary>
        private void OnEditPluginClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int index)
            {
                var plugin = _pluginsList.FirstOrDefault(p => p.Index == index)?.Plugin;
                if (plugin != null)
                {
                    var mainWindow = TopLevel.GetTopLevel(this) as MainAppWindow;
                    if (mainWindow != null)
                    {
                        var editPage = new PluginEditPage();
                        editPage.SetPlugin(plugin, "update");
                        mainWindow.NavigateToPage(editPage);
                    }
                }
            }
        }

        /// <summary>
        /// 删除插件
        /// </summary>
        private async void OnDeletePluginClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int index)
            {
                var result = await ShowConfirmDialogAsync("删除后不可恢复，您确定吗？");
                if (result)
                {
                    var response = await _pluginService.DeletePluginAsync(index);
                    if (response?.Code == 200)
                    {
                        await LoadPluginsAsync();
                    }
                }
            }
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        private async Task<bool> ShowConfirmDialogAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "确认删除",
                Content = message,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }
    }

    /// <summary>
    /// 插件视图模型
    /// </summary>
    public class PluginViewModel
    {
        public int Index { get; set; }
        public PluginType Plugin { get; set; } = new();
        public string MessageTypes { get; set; } = "";
    }
}
